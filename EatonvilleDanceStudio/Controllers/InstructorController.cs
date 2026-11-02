using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewModels;
using EatonvilleDanceStudio.Data;

namespace EatonvilleDanceStudio.Controllers;

// Locked down to satisfy the security requirement of Issue #66
[Authorize(Roles = "Instructor")]
public class InstructorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public InstructorController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    // GET
    /// <summary>
    /// Displays the instructor's dashboard, including their assigned classes, private lessons,
    /// today's schedule, upcoming schedule, and recent time logs.
    /// </summary>
    /// <returns>Returns the instructor dashboard view populated with relevant data.</returns>
    public async Task<IActionResult> Dashboard()
    {
        // Identify current logged in Instructor
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        
        ApplicationUser? instructor = await _userManager.FindByIdAsync(userId);
        if (instructor == null) return NotFound();
        
        // Fetch all classes assigned to this Instructor
        List<DanceClass> assignedClasses = await _context.DanceClasses
            .Where(c => c.InstructorId == userId)
            .ToListAsync();

        string today = DateTime.Today.DayOfWeek.ToString();
        
        // Fetch upcoming private lesson slots today or in the future
        List<PrivateLessonBlock> privateLessons = await _context
            .PrivateLessonBlocks
            .Where(p => p.InstructorId == userId && p.Date >= DateTime.Today)
            .OrderBy(p => p.Date)
            .ThenBy(p => p.StartTime)
            .ToListAsync();

        // Fetch the instructor's 10 most recent time logs
        List<InstructorTimeLog> recentLogs = await _context.InstructorTimeLogs
            .Where(t => t.InstructorId == userId)
            .OrderByDescending(t => t.Date)
            .Take(10)
            .ToListAsync();
        
        // Fill the ViewModel buckets
        InstructorDashboardViewModel viewModel = new InstructorDashboardViewModel
        {
            Instructor = instructor,
            AssignedClasses = assignedClasses,
            
            TodaysSchedule = assignedClasses
                .Where(c => c.DayOfWeek == today)
                .OrderBy(c => c.StartTime)
                .ToList(),
            
            UpcomingSchedule = assignedClasses
                .Where(c => c.DayOfWeek != today)
                .OrderBy(c => c.DayOfWeek)
                .ThenBy(c => c.StartTime)
                .ToList(),

            PrivateLessons = privateLessons,
            RecentTimeLogs = recentLogs
        };
        
        return View(viewModel);
    }
    
    // POST: /Instructor/LogHours
    /// <summary>
    /// Logs instructor work hours and submits them for approval by the director.
    /// </summary>
    /// <param name="model">The view model containing the hours worked, date, and optional description.</param>
    /// <returns>Redirects to the dashboard view upon successful submission or if validation errors occur.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogHours(InstructorDashboardViewModel model)
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (model.NewLog.Hours > 0 && model.NewLog.Date != default)
        {
            InstructorTimeLog timeLog = new InstructorTimeLog
            {
                InstructorId = userId,
                Date = model.NewLog.Date,
                Hours = model.NewLog.Hours,
                Description = model.NewLog.Description,
                IsApproved = false 
            };

            _context.InstructorTimeLogs.Add(timeLog);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = 
                "Hours successfully logged and sent to Director for approval.";
        }
        else
        {
            TempData["ErrorMessage"] = 
                "Please enter a valid date and hours worked.";
        }

        return RedirectToAction(nameof(Dashboard));
    }
    
    // POST: /Instructor/AddPrivateLesson
    /// <summary>
    /// Creates a new private lesson block for the instructor and stores it in the database
    /// if the provided date and time range are valid.
    /// </summary>
    /// <param name="model">The view model containing the details of the new private lesson block.</param>
    /// <returns>A redirect to the dashboard view. If the operation is unsuccessful,
    /// an error message is added to TempData.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPrivateLesson(
        InstructorDashboardViewModel model)
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        bool isValidDate = model.NewLessonBlock.Date >= DateTime.Today;
        bool isValidTime = model.NewLessonBlock.StartTime 
                           < model.NewLessonBlock.EndTime;

        if (isValidDate && isValidTime)
        {
            PrivateLessonBlock block = new PrivateLessonBlock
            {
                InstructorId = userId,
                Date = model.NewLessonBlock.Date,
                StartTime = model.NewLessonBlock.StartTime,
                EndTime = model.NewLessonBlock.EndTime,
                IsBooked = false
            };

            _context.PrivateLessonBlocks.Add(block);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Private lesson block opened!";
        }
        else
        {
            TempData["ErrorMessage"] = 
                "Invalid date/time range. Start time must be before end time.";
        }

        return RedirectToAction(nameof(Dashboard));
    }

    // GET: /Instructor/Roster/{id}
    /// <summary>
    /// Retrieves the roster for a specified dance class and date, displaying attendance records
    /// for all enrolled students.
    /// </summary>
    /// <param name="id">The identifier of the dance class.</param>
    /// <param name="date">The date for which the roster is being retrieved. If null, today's date is used.</param>
    /// <returns>An <see cref="IActionResult"/> that renders the class roster view or an error result if the class is not found or the user is unauthorized.</returns>
    public async Task<IActionResult> Roster(int id, DateTime? date)
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        DateTime selectedDate = date ?? DateTime.Today;

        DanceClass? danceClass = await _context.DanceClasses
            .Include(c => c.Enrollments)
                .ThenInclude(e => e.Dancer)
            .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == userId);

        if (danceClass == null) return NotFound();

        Dictionary<int, AttendanceRecord> existingRecords = await _context
            .AttendanceRecords
            .Where(a => a.Enrollment!.DanceClassId == id 
                        && a.Date == selectedDate)
            .ToDictionaryAsync(a => a.EnrollmentId);

        List<AttendanceRecord> attendanceList = new List<AttendanceRecord>();

        foreach (Enrollment enrollment in danceClass.Enrollments)
        {
            if (existingRecords.TryGetValue(
                enrollment.Id, out AttendanceRecord? existingRecord))
            {
                existingRecord.Enrollment = enrollment; 
                attendanceList.Add(existingRecord);
            }
            else
            {
                attendanceList.Add(new AttendanceRecord
                {
                    EnrollmentId = enrollment.Id,
                    Enrollment = enrollment, 
                    Date = selectedDate,
                    Status = "Present"
                });
            }
        }

        ClassRosterViewModel viewModel = new ClassRosterViewModel
        {
            DanceClass = danceClass,
            ClassDate = selectedDate,
            AttendanceList = attendanceList
        };

        return View(viewModel);
    }

    // POST: /Instructor/SubmitAttendance
    /// <summary>
    /// Processes and saves attendance records for a specific dance class and date.
    /// </summary>
    /// <param name="model">The view model containing the class details, date, and attendance records to be submitted.</param>
    /// <returns>An IActionResult that redirects to the Roster view with updated attendance data, or an Unauthorized result if the user ID is not found.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAttendance(ClassRosterViewModel model)
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        foreach (AttendanceRecord record in model.AttendanceList)
        {
            if (record.Id > 0)
            {
                _context.AttendanceRecords.Update(record);
            }
            else
            {
                _context.AttendanceRecords.Add(record);
            }
        }
        
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Attendance successfully saved!";
        
        return RedirectToAction(nameof(Roster), 
            new { id = model.DanceClass.Id, 
                  date = model.ClassDate.ToString("yyyy-MM-dd") });
    }
    
    // GET: /Instructor/Board/{id}
    /// <summary>
    /// Displays the board view for a specific dance class, including announcements and pagination.
    /// </summary>
    /// <param name="id">The identifier of the dance class.</param>
    /// <param name="page">The current page number for the paginated list of announcements. Defaults to 1.</param>
    /// <returns>A view displaying the dance class board, including its announcements and pagination details.</returns>
    public async Task<IActionResult> Board(int id, int page = 1)
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        DanceClass? danceClass = await _context.DanceClasses
            .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == userId);

        if (danceClass == null) return NotFound();

        int pageSize = 5; 
        int totalPosts = await _context.ClassAnnouncements
            .CountAsync(a => a.DanceClassId == id);
            
        int totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
        if (totalPages == 0) totalPages = 1;

        List<ClassAnnouncement> announcements = await _context
            .ClassAnnouncements
            .Where(a => a.DanceClassId == id)
            .OrderByDescending(a => a.DatePosted)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ClassBoardViewModel viewModel = new ClassBoardViewModel
        {
            DanceClass = danceClass,
            Announcements = announcements,
            CurrentPage = page,
            TotalPages = totalPages
        };

        return View(viewModel);
    }
    
    // POST: /Instructor/PostAnnouncement
    /// <summary>
    /// Handles the posting of a new announcement for a specific dance class.
    /// Validates that the instructor has permission to post for the specified class,
    /// creates a new announcement, and saves it to the database.
    /// </summary>
    /// <param name="model">The view model containing the details of the new announcement,
    /// including the message, title, and related class information.</param>
    /// <returns>A redirection to the class board view upon successful posting of the announcement,
    /// or an unauthorized response if the instructor is not permitted to post for the class.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostAnnouncement(ClassBoardViewModel model)
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        
        DanceClass? danceClass = await _context.DanceClasses
            .FirstOrDefaultAsync(c => c.Id == model.NewAnnouncement.DanceClassId 
                                      && c.InstructorId == userId);
        
        if (danceClass != null)
        {
            ClassAnnouncement safeAnnouncement = new ClassAnnouncement
            {
                DanceClassId = danceClass.Id, 
                Title = model.NewAnnouncement.Title,
                Message = model.NewAnnouncement.Message,
                DatePosted = DateTime.Now
            };

            _context.ClassAnnouncements.Add(safeAnnouncement);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Announcement successfully posted!";
        }
        else
        {
            TempData["ErrorMessage"] = 
                "You do not have permission to post for this class";
        }

        return RedirectToAction(nameof(Board),
            new { id = model.NewAnnouncement.DanceClassId });
    }

    // GET: /Instructor/Inbox
    /// <summary>
    /// Retrieves a list of messages from the studio inbox for the currently logged-in instructor.
    /// Messages include those sent by or received by the user, as well as messages with no specific receiver (e.g., announcements).
    /// </summary>
    /// <returns>Returns a view containing the list of studio messages ordered by the time they were sent.</returns>
    public async Task<IActionResult> Inbox()
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        List<StudioMessage> messages = await _context.StudioMessages
            .Include(m => m.Sender)
            // Fix: Check for m.ReceiverId == null instead of MessageType
            .Where(m => m.ReceiverId == userId || m.SenderId == userId || m.ReceiverId == null)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        return View(messages);
    }
}
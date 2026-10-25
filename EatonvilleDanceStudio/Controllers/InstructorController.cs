using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewModels;
using EatonvilleDanceStudio.Data;

namespace EatonvilleDanceStudio.Controllers;

public class InstructorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public InstructorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    //GET

    /// <summary>
    /// Displays the dashboard view for an instructor. The method retrieves the
    /// currently logged-in instructor's details and their assigned classes.
    /// It organizes the information into schedules for classes happening today
    /// and upcoming classes.
    /// </summary>
    /// <returns>The instructor's dashboard view populated with their details and class schedules.</returns>
    
    public async Task<IActionResult> Dashboard()
    {
        // Identify current logged in Instructor
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        
        var instructor = await _userManager.FindByIdAsync(userId);
        if (instructor == null) return NotFound();
        
        // Fetch all classes assigned to this Instructor
        var assignedClasses = await _context.DanceClasses
            .Where(c => c.InstructorId == userId)
            .ToListAsync();

        // Determine today's day of the week so the LINQ query below knows what 'today' is
        var today = DateTime.Today.DayOfWeek.ToString();
        
        // Fetch upcoming private lesson slots today or in the future
        var privateLessons = await _context.PrivateLessonBlocks
            .Where(p => p.InstructorId == userId && p.Date >= DateTime.Today)
            .OrderBy(p => p.Date)
            .ThenBy(p => p.StartTime)
            .ToListAsync();

        //  Fetch the instructor's 10 most recent time logs
        var recentLogs = await _context.InstructorTimeLogs
            .Where(t => t.InstructorId == userId)
            .OrderByDescending(t => t.Date)
            .Take(10)
            .ToListAsync();
        
        // Fill the ViewModel buckets
        var viewModel = new InstructorDashboardViewModel
        {
            Instructor = instructor,
            AssignedClasses = assignedClasses,
            
            // Classes happening today, ordered by start time.
            TodaysSchedule = assignedClasses
                .Where(c => c.DayOfWeek == today)
                .OrderBy(c => c.StartTime)
                .ToList(),
            
            // The list of upcoming classes, everything that is not happening today
            UpcomingSchedule = assignedClasses
                .Where(c => c.DayOfWeek != today)
                .OrderBy(c => c.DayOfWeek)
                .ThenBy(c => c.StartTime)
                .ToList(),

            // Actually pass the queried data into the View Model so the HTML can see it
            PrivateLessons = privateLessons,
            RecentTimeLogs = recentLogs
        };
        
        return View(viewModel);
    }
    
    // POST: /Instructor/LogHours

    /// <summary>
    /// Logs hours worked by the instructor. The method validates the
    /// submitted data and creates a new time log entry for the instructor.
    /// If the input is valid, the hours are saved and submitted for
    /// approval; otherwise, an error message is displayed.
    /// </summary>
    /// 
    /// <param name="model">The view model containing the instructor's
    /// input for logging worked hours, including the date, number of
    /// hours, and an optional description.</param>
    /// 
    /// <returns>The updated instructor dashboard view with a success
    /// or error message.</returns>
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogHours(InstructorDashboardViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        // Basic validation to ensure they didn't submit 0 hours or a blank date
        if (model.NewLog.Hours > 0 && model.NewLog.Date != default)
        {
            var timeLog = new InstructorTimeLog
            {
                InstructorId = userId,
                Date = model.NewLog.Date,
                Hours = model.NewLog.Hours,
                Description = model.NewLog.Description,
                IsApproved = false // Always defaults to false until Director reviews it
            };

            _context.InstructorTimeLogs.Add(timeLog);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Hours successfully logged and sent to the Director for approval.";
        }
        else
        {
            TempData["ErrorMessage"] = "Please enter a valid date and hours worked.";
        }

        return RedirectToAction(nameof(Dashboard));
    }
    
    // POST: /Instructor/AddPrivateLesson

    /// <summary>
    /// Adds a new private lesson block for the instructor. The method ensures the provided
    /// lesson block has a valid date and time range before saving it to the database.
    /// If the addition is successful, a success message is displayed; otherwise,
    /// an error message is returned with validation feedback.
    /// </summary>
    /// <param name="model">The view model containing details for the new private
    /// lesson block, including date, start time, and end time.</param>
    /// <returns>A redirect to the instructor's dashboard view, updated with the
    /// new private lesson block or feedback on input errors.</returns>
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPrivateLesson(InstructorDashboardViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        // Ensure they aren't adding past dates and start time is before end time
        if (model.NewLessonBlock.Date >= DateTime.Today && model.NewLessonBlock.StartTime < model.NewLessonBlock.EndTime)
        {
            var block = new PrivateLessonBlock
            {
                InstructorId = userId,
                Date = model.NewLessonBlock.Date,
                StartTime = model.NewLessonBlock.StartTime,
                EndTime = model.NewLessonBlock.EndTime,
                IsBooked = false // Defaults to false until someone books it
            };

            _context.PrivateLessonBlocks.Add(block);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Private lesson block successfully opened!";
        }
        else
        {
            TempData["ErrorMessage"] = "Invalid date or time range. Make sure the start time is before the end time.";
        }

        return RedirectToAction(nameof(Dashboard));
    }
    // GET: /Instructor/Roster/{id}

    /// <summary>
    /// Displays the roster view for a specific dance class and date. This method retrieves
    /// the enrolled dancers of the class, checks for any existing attendance records for the
    /// specified date, and prepares the attendance list for display.
    /// </summary>
    /// <param name="id">The unique identifier of the dance class.</param>
    /// <param name="date">The date for which the roster is being displayed. Defaults to today's date if not provided.</param>
    /// <returns>The roster view populated with the dance class details, selected date, and attendance list.</returns>
    
    public async Task<IActionResult> Roster(int id, DateTime? date)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var selectedDate = date ?? DateTime.Today;

        // Fetch the class, ensuring we also grab the Enrollments and the actual Dancers
        var danceClass = await _context.DanceClasses
            .Include(c => c.Enrollments)
                .ThenInclude(e => e.Dancer)
            .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == userId);

        if (danceClass == null) return NotFound();

        // Look up if attendance was already taken for this date
        var existingRecords = await _context.AttendanceRecords
            .Where(a => a.Enrollment!.DanceClassId == id && a.Date == selectedDate)
            .ToDictionaryAsync(a => a.EnrollmentId);

        var attendanceList = new List<AttendanceRecord>();

        // Build the roster row by row
        foreach (var enrollment in danceClass.Enrollments)
        {
            if (existingRecords.TryGetValue(enrollment.Id, out var existingRecord))
            {
                // Attendance was already taken, load it
                existingRecord.Enrollment = enrollment; // Attach for the view
                attendanceList.Add(existingRecord);
            }
            else
            {
                // No attendance taken yet, create a fresh "Present" record
                attendanceList.Add(new AttendanceRecord
                {
                    EnrollmentId = enrollment.Id,
                    Enrollment = enrollment, // Attach for the view
                    Date = selectedDate,
                    Status = "Present"
                });
            }
        }

        var viewModel = new ClassRosterViewModel
        {
            DanceClass = danceClass,
            ClassDate = selectedDate,
            AttendanceList = attendanceList
        };

        return View(viewModel);
    }

    // POST: /Instructor/SubmitAttendance

    /// <summary>
    /// Handles the submission of attendance records for a specific dance class on a given date.
    /// Updates existing attendance records or adds new ones based on the submitted data,
    /// and saves the changes to the database.
    /// </summary>
    /// <param name="model">The view model containing the dance class details, class date,
    /// and the list of attendance records to be processed.</param>
    /// <returns>A redirect to the roster page for the specified class and date, or an
    /// unauthorized response if the user is not logged in.</returns>
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAttendance(ClassRosterViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        // Loop through the submitted form data
        foreach (var record in model.AttendanceList)
        {
            if (record.Id > 0)
            {
                // This record already exists in the database; update it
                _context.AttendanceRecords.Update(record);
            }
            else
            {
                // This is a brand new record being saved for the first time
                _context.AttendanceRecords.Add(record);
            }
        }
        
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Attendance successfully saved!";
        
        // Redirect back to the roster page for the date they just edited
        return RedirectToAction(nameof(Roster), new { id = model.DanceClass.Id, date = model.ClassDate.ToString("yyyy-MM-dd") });
    }
    
    // GET

    /// <summary>
    /// Retrieves the details of a specific dance class and its associated announcements.
    /// Validates that the currently logged-in instructor is authorized to access the class.
    /// If valid, organizes the information into a view model for display.
    /// 
    /// </summary>
    /// <param name="id">The unique identifier of the dance class.</param>
    /// <returns>The class board view populated with class details and announcements if the
    /// instructor is authorized; otherwise, an unauthorized or not-found result.</returns>
    
    public async Task<IActionResult> Board(int id)
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        DanceClass? danceClass = await _context.DanceClasses
            .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == userId);

        if (danceClass == null) return NotFound();

        List<ClassAnnouncement> announcements = await _context.ClassAnnouncements
            .Where(a => a.DanceClassId == id)
            .OrderByDescending(a => a.DatePosted)
            .ToListAsync();

        ClassBoardViewModel viewModel = new ClassBoardViewModel
        {
            DanceClass = danceClass,
            Announcements = announcements
        };
        
        return View(viewModel);
    }
    
    //POST

    /// <summary>
    /// Handles the posting of a new announcement for a specific dance class.
    /// Validates the instructor's authorization to post for the targeted class,
    /// assigns a timestamp to the announcement, and saves it to the database.
    /// </summary>
    /// <param name="model">The view model containing the announcement details
    /// and the dance class to which the announcement belongs.</param>
    /// <returns>A redirection to the class board view, or an unauthorized result
    /// if the instructor is not permitted to post the announcement.</returns>
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostAnnouncement(ClassBoardViewModel model)
    {
        string? userId = _userManager.GetUserId(User);

        if (userId == null) return Unauthorized();
        
        // Security check, does class belong to instructor
        DanceClass? danceClass = await _context.DanceClasses
            .FirstOrDefaultAsync(c => c.Id == model.NewAnnouncement.DanceClassId && c.InstructorId == userId);
        
        if (danceClass != null)
        {
            // Construct a fresh entity server-side to prevent over-posting attacks.
            // Only extract the specific string values the user is allowed to edit.
            ClassAnnouncement safeAnnouncement = new ClassAnnouncement
            {
                DanceClassId = danceClass.Id, // Use the verified ID from the database
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
            TempData["ErrorMessage"] = "You do not have permission to post an announcement for this class";
        }

        return RedirectToAction(nameof(Board),
            new { id = model.NewAnnouncement.DanceClassId });
    }
}
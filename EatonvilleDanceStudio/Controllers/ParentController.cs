using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Controllers;

/// <summary>
/// ParentController handles interactions related to parental user functionality within the application.
/// This includes retrieving and managing parent profiles, associated dancers, and their enrollments.
/// </summary>

// [Issue #67] Locked this down specifically to the Parent role
[Authorize(Roles = "Parent")] 
public class ParentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ParentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Parent/Dashboard
    /// <summary>
    /// Displays the main parent dashboard including financial balances, linked dancers,
    /// and an aggregated weekly class schedule.
    /// </summary>
    public async Task<IActionResult> Dashboard()
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        // Fetch the Parent Profile
        ParentProfile? profile = await _context.ParentProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return NotFound("Parent profile not found. Please contact the studio.");
        }
        
        bool hasSignedWaiver = await _context.LegalWaivers
            .AnyAsync(w => w.ParentProfileId == profile.Id && w.Season == "2026-2027");

        if (!hasSignedWaiver)
        {
            // Force them to sign page immediately
            return RedirectToAction("SignWaiver");
        }

        // Fetch all Dancers belonging to this household
        List<Dancer> dancers = await _context.Dancers
            .Where(d => d.ParentProfileId == profile.Id)
            .ToListAsync();

        List<int> dancerIds = dancers.Select(d => d.Id).ToList();

        // Consolidate the weekly schedule across all linked dancers
        List<Enrollment> enrolledClasses = await _context.Enrollments
            .Include(e => e.DanceClass)
            .Include(e => e.Dancer)
            .Where(e => dancerIds.Contains(e.DancerId)) 
            .OrderBy(e => e.DanceClass.DayOfWeek)       
            .ToListAsync();

        // Calculate Financials based on LedgerEntry schema
        List<LedgerEntry> allEntries = await _context.LedgerEntries
            .Where(l => l.ParentId == userId)          // Links via ApplicationUser ID
            .OrderByDescending(l => l.TransactionDate) // Uses TransactionDate
            .ToListAsync();

        // Charges are positive amounts, Payments are negative amounts
        List<LedgerEntry> recentCharges = allEntries.Where(l => l.Amount > 0).Take(5).ToList();
        List<LedgerEntry> recentPayments = allEntries.Where(l => l.Amount < 0).Take(5).ToList();

        // Since payments are already negative, we just sum everything together to get the outstanding balance!
        decimal calculatedBalance = allEntries.Sum(l => l.Amount);
        
        // Fetch Direct Messages & Studio Blasts for this Parent
        List<StudioMessage> messages = await _context.StudioMessages
            .Include(m => m.Sender) // <-- ADD THIS LINE!
            .Where(m => m.ReceiverId == userId || m.ReceiverId == null)
            .OrderByDescending(m => m.SentAt)
            .Take(5) 
            .ToListAsync();
        
        // Build the ViewModel (Only declared once)
        ParentDashboardViewModel viewModel = new ParentDashboardViewModel
        {
            Profile = profile,
            Dancers = dancers,
            EnrolledClasses = enrolledClasses,
            CurrentBalance = calculatedBalance,
            RecentCharges = recentCharges,
            RecentPayments = recentPayments,
            Messages = messages // Includes direct messages and studio blasts
        };

        return View(viewModel);
    }
    
    // POST: /Parent/SendMessage
    /// <summary>
    /// Sends a message from the currently logged-in parent to the specified recipient.
    /// The message includes a subject, body content, and metadata such as sender and timestamp.
    /// </summary>
    /// <param name="receiverId">The unique identifier of the user receiving the message.</param>
    /// <param name="subject">The subject line of the message. Prefixed with "Re:" if not already present.</param>
    /// <param name="body">The content of the message body. Cannot be empty or whitespace.</param>
    /// <returns>Redirects to the "Dashboard" view. Displays a success message if the operation is successful, or an error message if the body is invalid.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(
        string receiverId,
        string subject,
        string body)
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(receiverId) && 
            !string.IsNullOrWhiteSpace(body))
        {
            StudioMessage reply = new StudioMessage
            {
                SenderId = userId,
                ReceiverId = receiverId,
                Subject = subject.StartsWith("Re:") 
                    ? subject : "Re: " + subject,
                Body = body,
                SentAt = DateTime.Now,
                IsRead = false
            };
            
            _context.StudioMessages.Add(reply);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Message sent successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Message body cannot be empty.";
        }

        return RedirectToAction(nameof(Dashboard));
    }

    // GET: /Parent/Board/{id}
    /// <summary>
    /// Displays the class board for a specific dance class, allowing parents to
    /// view related announcements.
    /// Ensures that the authenticated parent has a dancer enrolled in the
    /// selected class before granting access.
    /// </summary>
    public async Task<IActionResult> Board(int id)
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        // Fetch the Parent Profile first to get the integer ID
        ParentProfile? profile = await _context.ParentProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            TempData["ErrorMessage"] = "Parent profile not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        // Security check: Verify the parent has a dancer in this class
        // Now we compare the integer ParentProfileId to our profile.Id
        bool isEnrolled = await _context.Enrollments
            .Include(e => e.Dancer)
            .AnyAsync(e => e.DanceClassId == id && 
                           e.Dancer != null && 
                           e.Dancer.ParentProfileId == profile.Id);

        if (!isEnrolled)
        {
            TempData["ErrorMessage"] = "You do not have access to this board.";
            return RedirectToAction(nameof(Dashboard));
        }

        DanceClass? danceClass = await _context.DanceClasses
            .FirstOrDefaultAsync(c => c.Id == id);

        if (danceClass == null) return NotFound();

        List<ClassAnnouncement> announcements = await _context
            .ClassAnnouncements
            .Where(a => a.DanceClassId == id)
            .OrderByDescending(a => a.DatePosted)
            .ToListAsync();

        ParentClassBoardViewModel viewModel = new ParentClassBoardViewModel
        {
            DanceClass = danceClass,
            Announcements = announcements
        };

        return View(viewModel);
    }

    // GET: /Parent/AddDancer
    /// <summary>
    /// Renders the form for adding a new dancer to the parent's profile.
    /// </summary>
    public IActionResult AddDancer()
    {
        return View(new AddDancerViewModel { FirstName = "", LastName = "" });
    }

    // POST: /Parent/AddDancer
    /// <summary>
    /// Adds a new dancer associated with the authenticated parent's profile.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDancer(AddDancerViewModel input)
    {
        var userId = _userManager.GetUserId(User);
        var parentProfile = await _context.ParentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (parentProfile == null) return NotFound();

        if (ModelState.IsValid)
        {
            var newDancer = new Dancer
            {
                FirstName = input.FirstName,
                LastName = input.LastName,
                DateOfBirth = input.DateOfBirth,
                SkillLevel = input.SkillLevel,
                ParentProfileId = parentProfile.Id,
                PhotoApprovalStatus = "None",
                ProfilePhotoPath = null,
                MedicalAlerts = input.MedicalAlerts
            };

            _context.Dancers.Add(newDancer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }

        return View(input);
    }
    
    // GET: /Parent/SignWaiver
    /// <summary>
    /// Displays the legal waiver signing form for the current parent with a preconfigured season and waiver type.
    /// </summary>
    /// <returns>
    /// A view containing the initialized legal waiver, or an error result if the parent account or profile cannot be found.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> SignWaiver()
    {
        ApplicationUser? user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        ParentProfile? profile = await _context.ParentProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        if (profile == null) return NotFound("Parent profile not found.");

        LegalWaiver newWaiver = new LegalWaiver
        {
            ParentProfileId = profile.Id,
            Season = "2026-2027", 
            WaiverType = "Master Liability, Medical & Photo Release"
        };

        return View(newWaiver);
    }

    // POST: /Parent/SignWaiver
    /// <summary>
    /// Processes a submitted legal waiver, records the signing date and client IP address,
    /// saves the waiver, and redirects the parent to the dashboard when valid.
    /// </summary>
    /// <param name="model">The legal waiver information submitted by the parent.</param>
    /// <returns>The dashboard redirect after a successful submission, or the waiver view with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignWaiver(LegalWaiver model)
    {
        // Remove navigation property from validation
        ModelState.Remove(nameof(model.ParentProfile));

        if (ModelState.IsValid)
        {
            // Capture exact time and IP for the legal ledger
            model.SignedDate = DateTime.Now;
            model.IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _context.LegalWaivers.Add(model);
            await _context.SaveChangesAsync();

            // Send them back to their dashboard once signed
            return RedirectToAction(nameof(Dashboard));
        }

        return View(model);
    }
}
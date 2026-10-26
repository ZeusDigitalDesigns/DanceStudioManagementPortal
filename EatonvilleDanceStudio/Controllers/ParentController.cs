using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewModels;
using EatonvilleDanceStudio.Views.Instructor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Controllers;

/// <summary>
/// ParentController handles interactions related to parental user functionality within the application.
/// This includes retrieving and managing parent profiles, associated dancers, and their enrollments.
/// </summary>
[Authorize] // Locks this down to only logged-in users
public class ParentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ParentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Parent/Board/{id}

    /// <summary>
    /// Displays the class board for a specific dance class, allowing parents to
    /// view related announcements.
    /// Ensures that the authenticated parent has a dancer enrolled in the
    /// selected class before granting access.
    /// </summary>
    /// <param name="id">The unique identifier of the dance class to display.</param>
    /// <returns>A view populated with class details and announcements if the
    /// parent has access; otherwise, redirects to an appropriate action or error response.</returns>
    
    public async Task<IActionResult> Board(int id)
    {
        string? userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        // Security check: Verify the parent has a dancer in this class
        bool isEnrolled = await _context.Enrollments
            .Include(e => e.Dancer)
            .AnyAsync(e => e.DanceClassId == id && 
                           e.Dancer != null && 
                           e.Dancer.ParentId == userId);

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

    // GET
    /// <summary>
    /// Renders the form for adding a new dancer to the parent's profile.
    /// The form allows input of dancer details such as first name, last name, date of birth, and skill level.
    /// </summary>
    /// <returns>A view representing the add dancer form with an empty model.</returns>
    public IActionResult AddDancer()
    {
        return View(new AddDancerViewModel { FirstName = "", LastName = "" });
    }

    // POST 
    /// <summary>
    /// Adds a new dancer associated with the authenticated parent's profile,
    /// using the provided input data and ensuring model validation.
    /// </summary>
    /// <param name="input">The data required to create a new dancer, including first name, last name, date of birth, and skill level.</param>
    /// <returns>A task that represents the asynchronous operation. Returns a redirect to the dashboard upon success, or the input form view on validation failure.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDancer(AddDancerViewModel input)
    {
        var userId = _userManager.GetUserId(User);
        var parentProfile = await _context.ParentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (parentProfile == null) return NotFound();

        if (ModelState.IsValid)
        {
            // We manually map the safe fields from the Input Model to the Database Model
            var newDancer = new Dancer
            {
                FirstName = input.FirstName,
                LastName = input.LastName,
                DateOfBirth = input.DateOfBirth,
                SkillLevel = input.SkillLevel,

                // These are set strictly by the server, totally immune to user forging
                ParentProfileId = parentProfile.Id,
                PhotoApprovalStatus = "None",
                ProfilePhotoPath = null
            };

            _context.Dancers.Add(newDancer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }

        return View(input);
    }
}
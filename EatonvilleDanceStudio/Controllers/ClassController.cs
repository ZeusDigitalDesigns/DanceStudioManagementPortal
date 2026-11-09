using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewModels;

namespace EatonvilleDanceStudio.Controllers;

[Authorize]
public class ClassController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClassController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Class
    /// <summary>
    /// Displays the class browsing page for the authenticated parent.
    /// Verifies that the parent profile exists and that the account is approved,
    /// then loads available dance classes with instructor details and the parent's dancers.
    /// </summary>
    /// <return>An IActionResult that renders the class browsing view with available classes and dancers,
    /// or returns an error response if the parent profile is not found or the account is not approved.</return>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var parentProfile = await _context.ParentProfiles.Include(p => p.Dancers)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (parentProfile == null) return NotFound("Parent profile not found");

        if (parentProfile.AccountStatus != "Approved")
        {
            return Unauthorized("Your account must be approved by the studio director before you can browse classes.");
        }

        // Include the Instructor and sort the classes for the catalog
        var classes = await _context.DanceClasses
            .Include(c => c.Instructor)
            .OrderBy(c => c.ClassName)
            .ThenBy(c => c.ClassName)
            .ToListAsync();

        var viewModel = new ClassBrowseViewModel
        {
            AvailableClasses = classes,
            MyDancers = parentProfile.Dancers.ToList()
        };

        return View(viewModel);
    }

    // POST
    /// <summary>
    /// Enrolls a dancer into a specified dance class.
    /// Validates the user's permissions, account status,
    /// and class capacity before proceeding with the enrollment.
    /// </summary>
    /// <param name="classId">The unique identifier of the dance class to enroll in.</param>
    /// <param name="dancerId">The unique identifier of the dancer to be enrolled.</param>
    /// <returns>An IActionResult that redirects to the index page with a success or error message,
    /// or an error response if the operation fails validation checks or encounters an issue.</returns>

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int classId, int dancerId)
    {
        var userId = _userManager.GetUserId(User);

        var parentProfile = await _context.ParentProfiles
            .Include(p => p.Dancers)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (parentProfile == null || !parentProfile.Dancers.Any(d => d.Id == dancerId))
        {
            return Unauthorized("You do not have permission to enroll this dancer.");
        }

        if (parentProfile.AccountStatus != "Approved")
        {
            return Unauthorized(
                "Your account must be approved by the studio director before you can enroll in classes.");
        }

        // Start a strict transaction to prevent concurrent race conditions
        await using var transaction =
            await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            // 1. Prevent Double Enrollment
            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.DanceClassId == classId && e.DancerId == dancerId);

            if (alreadyEnrolled)
            {
                TempData["ErrorMessage"] = "Dancer is already enrolled or waitlisted in this class.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Load the Class to check Capacity
            var danceClass = await _context.DanceClasses
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (danceClass == null) return NotFound("Class not found.");

            // 3. Enforce Capacity & Assign Status
            var currentActiveEnrollments = danceClass.Enrollments.Count(e => e.Status == "Enrolled");
            var enrollmentStatus = currentActiveEnrollments >= danceClass.Capacity ? "Waitlisted" : "Enrolled";

            // 4. Create the Enrollment
            var enrollment = new Enrollment
            {
                DanceClassId = classId,
                DancerId = dancerId,
                Status = enrollmentStatus
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // 5. Lock it in
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = enrollmentStatus == "Enrolled"
                ? "Successfully enrolled in class!"
                : "Class is full. Dancer has been added to the waitlist.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "An error occurred while processing your enrollment. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }
}
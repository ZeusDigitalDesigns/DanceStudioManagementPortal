using System.Linq;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Controllers;

// [Issue #68] Locked down specifically to the Dancer role
[Authorize(Roles = "Dancer")]
public class DancerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    : Controller
{
    //GET 
    /// <summary>
    /// Displays the dashboard for the authenticated dancer, including the dancer
    /// profile and enrolled dance classes.
    /// </summary>
    /// <return>
    /// A task that represents the asynchronous operation. The result is an action
    /// result containing the dashboard view, an unauthorized result if no user
    /// is authenticated, or a not found result if the dancer profile cannot be located.
    /// </return>
    public async Task<IActionResult> Dashboard()
    {
        string? userId = userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        
        //Fetch the Dancer Profile
        Dancer? dancer = await context.Dancers
            .Include(d => d.Enrollments)
            .ThenInclude(e => e.DanceClass)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (dancer == null)
        {
            return NotFound("Dancer profile not found. Please contact the studio director.");
        }
        
        return View(dancer);
    
        
        // TODO: Fetch Video Vault links
    }
}
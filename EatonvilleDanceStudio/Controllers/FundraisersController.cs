using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Controllers;

/// <summary>
/// Provides administrative actions for managing fundraiser events, including listing, creating, editing, and deleting fundraisers.
/// </summary>
/// <remarks>
/// Access is restricted to users in the Director or Admin roles. The controller maintains fundraiser event details, financial totals, and related public bulletin board information.
/// </remarks>
[Authorize(Roles = "Director,Admin")]
public class FundraisersController : Controller
{
    private readonly ApplicationDbContext context;

    public FundraisersController(ApplicationDbContext dbContext)
    {
        context = dbContext;
    }

    /// <summary>
    /// Retrieves all fundraiser events ordered by event date and displays them in the index view.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation, containing the action result for the
    /// fundraiser events index view.
    /// </returns>
    public async Task<IActionResult> Index()
    {
        List<FundraiserEvent> events = await context.FundraiserEvents
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return View(events);
    }

    /// <summary>
    /// Displays the form for creating a new fundraiser event with default fundraiser details.
    /// </summary>
    /// <returns>
    /// The action result for the fundraiser creation view.
    /// </returns>
    public IActionResult Create()
    {
        FundraiserEvent defaultModel = new FundraiserEvent();
        return View(defaultModel);
    }

    /// <summary>
    /// Creates a new fundraiser event, assigns the current director as its creator,
    /// calculates net profit, and saves the event when validation succeeds.
    /// </summary>
    /// <param name="fundraiser">The fundraiser event data to create.</param>
    /// <returns>A task that represents the asynchronous operation, containing the
    /// action result that redisplays the form when validation fails or redirects
    /// to the index view when creation succeeds.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FundraiserEvent fundraiser)
    {
        string? directorId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);
        fundraiser.CreatedByDirectorId = directorId ?? "director";
        fundraiser.NetProfit =
            fundraiser.GrossRevenue - fundraiser.Expenses;

        ModelState.Remove(nameof(fundraiser.CreatedByDirectorId));
        ModelState.Remove(nameof(fundraiser.LedgerEntries));

        if (!ModelState.IsValid)
        {
            return View(fundraiser);
        }

        context.FundraiserEvents.Add(fundraiser);
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Fundraiser event created!";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Retrieves the fundraiser event matching the specified identifier and displays it in the edit view.
    /// </summary>
    /// <param name="id">The identifier of the fundraiser event to edit.</param>
    /// <returns>A task that represents the asynchronous operation, containing the
    /// action result for the edit view or a not found result if the identifier is missing
    /// or no matching fundraiser event exists.</returns>
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        FundraiserEvent? fundraiser =
            await context.FundraiserEvents.FindAsync(id);
        if (fundraiser == null) return NotFound();

        return View(fundraiser);
    }

    /// <summary>
    /// Updates an existing fundraiser event with the submitted event details,
    /// financial values, and profit split settings.
    /// </summary>
    /// <param name="id">The identifier of the fundraiser event to update.</param>
    /// <param name="fundraiser">The fundraiser event data submitted from the edit form.</param>
    /// <returns>
    /// A task that represents the asynchronous operation,
    /// containing the action result for the update request.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        FundraiserEvent fundraiser)
    {
        if (id != fundraiser.Id) return NotFound();

        ModelState.Remove(nameof(fundraiser.CreatedByDirectorId));
        ModelState.Remove(nameof(fundraiser.LedgerEntries));

        if (!ModelState.IsValid)
        {
            return View(fundraiser);
        }

        FundraiserEvent? existing =
            await context.FundraiserEvents.FindAsync(id);
        if (existing == null) return NotFound();

        // Update public Community Bulletin Board fields
        existing.EventName = fundraiser.EventName;
        existing.EventDate = fundraiser.EventDate;
        existing.CategoryBadge = fundraiser.CategoryBadge;
        existing.TimeAndLocation = fundraiser.TimeAndLocation;
        existing.Description = fundraiser.Description;
        existing.IsActive = fundraiser.IsActive;

        // Update Director financial & profit-split fields
        existing.GrossRevenue = fundraiser.GrossRevenue;
        existing.Expenses = fundraiser.Expenses;
        existing.NetProfit =
            fundraiser.GrossRevenue - fundraiser.Expenses;
        existing.SplitMode = fundraiser.SplitMode;

        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Fundraiser event updated!";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Deletes the fundraiser event with the specified identifier, saves the change,
    /// and redirects to the fundraiser events index view.
    /// </summary>
    /// <param name="id">The identifier of the fundraiser event to delete.</param>
    /// <returns>A task that represents the asynchronous operation, containing the
    /// action result that redirects to the index view.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        FundraiserEvent? fundraiser =
            await context.FundraiserEvents.FindAsync(id);

        if (fundraiser != null)
        {
            context.FundraiserEvents.Remove(fundraiser);
            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Fundraiser event removed.";
        }

        return RedirectToAction(nameof(Index));
    }
}
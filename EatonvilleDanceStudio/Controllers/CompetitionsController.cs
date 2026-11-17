using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Controllers
{
    /// <summary>
    /// MVC controller for managing dance competition information, including competition listings,
    /// detailed itineraries, routine entries, and attendance status.
    /// </summary>
    /// <remarks>
    /// Requires authenticated access. Creation, entry management, and attendance changes are
    /// restricted to users in director or instructor roles.
    /// </remarks>
    [Authorize]
    public class CompetitionsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment) : Controller
    {
        // GET: /Competitions?attendingOnly=true
        /// <summary>
        /// Displays the competitions list, ordered by start date and including related entries, optionally filtering to competitions the studio is attending.
        /// </summary>
        /// <param name="attendingOnly">A value indicating whether to show only competitions the studio is attending.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the view result with the filtered list of competitions.</returns>
        [HttpGet]
        public async Task<IActionResult> Index(
            bool attendingOnly = true)
        {
            IQueryable<Competition> query = context.Competitions
                .Include(c => c.Entries)
                .OrderBy(c => c.StartDate);

            if (attendingOnly)
            {
                query = query.Where(c => c.IsStudioAttending);
            }

            ViewBag.AttendingOnly = attendingOnly;
            List<Competition> competitions = await query.ToListAsync();
            return View(competitions);
        }

        // GET: /Competitions/Details/5
        /// <summary>
        /// Displays the details for a competition, including its entries and related dance classes, with entries ordered by performance order and backstage call time.
        /// </summary>
        /// <param name="id">The identifier of the competition to display.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the view result for the competition, or a not found result if no matching competition exists.</returns>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            Competition? competition = await context.Competitions
                .Include(c => c.Entries)
                    .ThenInclude(e => e.DanceClass)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (competition == null)
            {
                return NotFound();
            }

            competition.Entries = competition.Entries
                .OrderBy(e => e.PerformanceOrder)
                .ThenBy(e => e.BackstageCallTime)
                .ToList();

            return View(competition);
        }

        // GET: /Competitions/Create
        /// <summary>
        /// Displays the competition creation form with default attendance selected for the studio.
        /// </summary>
        /// <returns>The view result containing a new competition model initialized for creation.</returns>
        [HttpGet]
        [Authorize(Roles = "Director,Instructor")]
        public IActionResult Create()
        {
            
            Competition model = new Competition
            {
                IsStudioAttending = true
            };
            return View(model);
        }

        // POST: /Competitions/Create
        /// <summary>
        /// Creates a new competition and saves an uploaded banner or
        /// logo image to wwwroot/images/competitions (#55).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Director,Instructor")]
        public async Task<IActionResult> Create(
            Competition model,
            IFormFile? logoFile)
        {
            ModelState.Remove(nameof(model.Entries));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (logoFile != null && logoFile.Length > 0)
            {
                model.LogoUrl =
                    await SaveCompetitionImageAsync(logoFile);
            }

            if (string.IsNullOrWhiteSpace(model.BrandColorHex))
            {
                model.BrandColorHex = "#ff10f0";
            }
            
            if (!model.FeeDueDate.HasValue)
            {
                model.FeeDueDate = model.StartDate.AddDays(-14);
            }
            
            context.Competitions.Add(model);
            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id = model.Id });
        }

        // POST: /Competitions/Edit/5
        /// <summary>
        /// Updates an existing competition and replaces or keeps its
        /// uploaded banner/logo image (#55).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Director,Instructor")]
        public async Task<IActionResult> Edit(
            int id,
            Competition model,
            IFormFile? logoFile)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(model.Entries));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Competition? existing = await context.Competitions
                .FindAsync(id);

            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = model.Name;
            existing.EventFormat = model.EventFormat;
            existing.VenueName = model.VenueName;
            existing.VenueAddress = model.VenueAddress;
            existing.StartDate = model.StartDate;
            existing.EndDate = model.EndDate;
            existing.IsStudioAttending = model.IsStudioAttending;
            existing.Notes = model.Notes;
            existing.RegistrationUrl = model.RegistrationUrl;
            existing.BrandColorHex =
                string.IsNullOrWhiteSpace(model.BrandColorHex)
                    ? "#ff10f0"
                    : model.BrandColorHex;
            existing.IsFeaturedAnnouncement =
                model.IsFeaturedAnnouncement;
            existing.RegistrationFee = model.RegistrationFee;
            existing.FeeDueDate = model.FeeDueDate ?? model.StartDate.AddDays(-14);

            // If a new file was uploaded, save it; otherwise keep
            // the existing image or manual URL path
            if (logoFile != null && logoFile.Length > 0)
            {
                existing.LogoUrl =
                    await SaveCompetitionImageAsync(logoFile);
            }
            else if (!string.IsNullOrWhiteSpace(model.LogoUrl))
            {
                existing.LogoUrl = model.LogoUrl;
            }

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{existing.Name} updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = existing.Id });
        }

        // GET: /Competitions/AddEntry?competitionId=5
        /// <summary>
        /// Displays the form for adding a new entry to the specified competition, initializing default entry details and class selection data.
        /// </summary>
        /// <param name="competitionId">The identifier of the competition to add the entry to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the entry creation view, or a not found result if the competition does not exist.</returns>
        [HttpGet]
        [Authorize(Roles = "Director,Instructor")]
        public async Task<IActionResult> AddEntry(int competitionId)
        {
            Competition? competition = await context.Competitions
                .FindAsync(competitionId);

            if (competition == null)
            {
                return NotFound();
            }

            await PopulateClassesDropdownAsync(null);
            ViewBag.CompetitionName = competition.Name;

            CompetitionEntry model = new CompetitionEntry
            {
                CompetitionId = competitionId,
                BackstageCallTime = competition.StartDate.AddHours(8)
            };

            return View(model);
        }

        // POST: /Competitions/AddEntry
        /// <summary>
        /// Adds a competition entry using the submitted model, redisplaying the entry form when validation fails and redirecting to the competition details page after saving.
        /// </summary>
        /// <param name="model">The competition entry data to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the view result for invalid input or a redirect to the competition details page after the entry is saved.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Director,Instructor")]
        public async Task<IActionResult> AddEntry(
            CompetitionEntry model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateClassesDropdownAsync(model.DanceClassId);
                return View(model);
            }

            context.CompetitionEntries.Add(model);
            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id = model.CompetitionId });
        }

        // POST: /Competitions/ToggleAttendance/5
        /// <summary>
        /// Toggles whether the studio is attending the specified competition and redirects to the competitions list using the current attendance filter.
        /// </summary>
        /// <param name="id">The identifier of the competition whose attendance status is changed.</param>
        /// <param name="attendingOnly">A value indicating whether the redirected competitions list should show only competitions the studio is attending.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a redirect to the competitions index action.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Director,Instructor")]
        public async Task<IActionResult> ToggleAttendance(
            int id,
            bool attendingOnly)
        {
            Competition? competition = await context.Competitions
                .FindAsync(id);

            if (competition != null)
            {
                competition.IsStudioAttending =
                    !competition.IsStudioAttending;
                await context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Index),
                new { attendingOnly });
        }

        /// <summary>
        /// Displays the edit form for the specified competition, assigning a default brand color when none is set.
        /// </summary>
        /// <param name="id">The identifier of the competition to edit.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the edit view for the competition, or a not found result if no matching competition exists.</returns>
        [HttpGet]
        [Authorize(Roles = "Director,Instructor")]
        public async Task<IActionResult> Edit(int id)
        {
            Competition? competition = await context.Competitions.FindAsync(id);

            if (competition == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(competition.BrandColorHex))
            {
                competition.BrandColorHex = "#ff10f0";
            }
            return View(competition);
        }
        
        

        // POST: /Competitions/ToggleFeatured/5
        /// <summary>
        /// Quickly toggles whether a competition is displayed in the
        /// dynamic announcement banner across dashboards.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Director,Instructor")]
        public async Task<IActionResult> ToggleFeatured(
            int id,
            bool attendingOnly)
        {
            Competition? competition = await context.Competitions
                .FindAsync(id);

            if (competition != null)
            {
                competition.IsFeaturedAnnouncement =
                    !competition.IsFeaturedAnnouncement;
                await context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Index),
                new { attendingOnly });
        }

        // POST: /Competitions/Delete/5
        /// <summary>
        /// Deletes the specified competition and any associated competition entries, then redirects to the competitions list.
        /// </summary>
        /// <param name="id">The identifier of the competition to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a redirect to the competitions index action.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Director")]
        public async Task<IActionResult> Delete(int id)
        {
            Competition? competition = await context.Competitions
                .Include(c => c.Entries)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (competition != null)
            {
                if (competition.Entries.Any())
                {
                    context.CompetitionEntries
                        .RemoveRange(competition.Entries);
                }

                context.Competitions.Remove(competition);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"{competition.Name} removed.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Populates the dance classes dropdown list with classes ordered by name and applies the specified selected class when provided.
        /// </summary>
        /// <param name="selectedClassId">The identifier of the dance class to mark as selected, or null if no class should be selected.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task PopulateClassesDropdownAsync(
            int? selectedClassId)
        {
            List<DanceClass> classes = await context.DanceClasses
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            ViewBag.DanceClasses = new SelectList(
                classes,
                "Id",
                "ClassName",
                selectedClassId);
        }
        
        /// <summary>
        /// Saves an uploaded competition banner or logo image to
        /// wwwroot/images/competitions and returns its relative URL.
        /// </summary>
        private async Task<string?> SaveCompetitionImageAsync(
            IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            string uploadsFolder = Path.Combine(
                webHostEnvironment.WebRootPath,
                "images",
                "competitions");

            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName =
                Guid.NewGuid().ToString()
                + "_"
                + Path.GetFileName(file.FileName);

            string filePath = Path.Combine(
                uploadsFolder,
                uniqueFileName);

            using (FileStream fileStream = new FileStream(
                       filePath,
                       FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/images/competitions/" + uniqueFileName;
        }
    }
}
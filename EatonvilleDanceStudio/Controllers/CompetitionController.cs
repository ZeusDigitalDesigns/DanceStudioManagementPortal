using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Authorization;
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
    public class CompetitionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompetitionsController(ApplicationDbContext context)
        {
            _context = context;
        }

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
            IQueryable<Competition> query = _context.Competitions
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
            Competition? competition = await _context.Competitions
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
        /// Creates a new competition when the submitted model is valid, saves it, and redirects to its details page.
        /// </summary>
        /// <param name="model">The competition data submitted for creation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the original view when validation fails, or a redirect to the created competition details page when creation succeeds.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Director,Instructor")]
        public async Task<IActionResult> Create(Competition model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _context.Competitions.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id = model.Id });
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
            Competition? competition = await _context.Competitions
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

            _context.CompetitionEntries.Add(model);
            await _context.SaveChangesAsync();

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
            Competition? competition = await _context.Competitions
                .FindAsync(id);

            if (competition != null)
            {
                competition.IsStudioAttending =
                    !competition.IsStudioAttending;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Index),
                new { attendingOnly });
        }

        private async Task PopulateClassesDropdownAsync(
            int? selectedClassId)
        {
            List<DanceClass> classes = await _context.DanceClasses
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            ViewBag.DanceClasses = new SelectList(
                classes,
                "Id",
                "ClassName",
                selectedClassId);
        }
    }
}
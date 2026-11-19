using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Controllers
{
    /// <summary>
    /// Displays public 1-on-1 private lesson options and open
    /// availability blocks from the studio database.
    /// </summary>
    [AllowAnonymous]
    public class PrivateLessonsController : Controller
    {
        private readonly ApplicationDbContext databaseContext;

        public PrivateLessonsController(
            ApplicationDbContext context)
        {
            databaseContext = context;
        }

        // GET: /PrivateLessons
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<PrivateLessonBlock> openBlocks =
                await databaseContext.PrivateLessonBlocks
                    .Include(b => b.Instructor)
                    .Where(b => !b.IsBooked && b.Date >= DateTime.Today)
                    .OrderBy(b => b.Date)
                    .ThenBy(b => b.StartTime)
                    .ToListAsync();

            return View(openBlocks);
        }
    }
}
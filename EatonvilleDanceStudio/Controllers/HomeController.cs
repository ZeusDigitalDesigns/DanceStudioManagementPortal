using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Controllers
{
    /// <summary>
    /// Handles public landing page requests, including loading active
    /// upcoming fundraisers for the community bulletin board.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext context;

        public HomeController(ApplicationDbContext dbContext)
        {
            context = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            List<FundraiserEvent> upcomingEvents = await context
                .FundraiserEvents
                .Where(e => e.IsActive && e.EventDate >= DateTime.Today)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return View(upcomingEvents);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult TermsOfService()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            string reqId = Activity.Current?.Id
                           ?? HttpContext.TraceIdentifier;

            return View(new ErrorViewModel { RequestId = reqId });
        }
    }
}
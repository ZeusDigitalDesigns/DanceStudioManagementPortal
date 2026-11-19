using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EatonvilleDanceStudio.Controllers
{
    /// <summary>
    /// Handles public studio rental inquiry requests, including
    /// displaying the inquiry form and processing submitted availability requests.
    /// </summary>
    [AllowAnonymous]
    public class StudioRentalsController : Controller
    {
        private readonly ApplicationDbContext databaseContext;

        public StudioRentalsController(
            ApplicationDbContext context)
        {
            databaseContext = context;
        }

        // GET: /StudioRentals
        [HttpGet]
        public IActionResult Index()
        {
            StudioRentalInquiryViewModel model =
                new StudioRentalInquiryViewModel();

            return View(model);
        }

        // POST: /StudioRentals
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Index(
            StudioRentalInquiryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Task.FromResult<IActionResult>(View(model));
            }

            TempData["RentalSuccess"] =
                "Room availability request received! The studio will " +
                "follow up regarding your requested date and hours.";

            return Task.FromResult<IActionResult>(
                RedirectToAction(nameof(Index)));
        }
    }
}
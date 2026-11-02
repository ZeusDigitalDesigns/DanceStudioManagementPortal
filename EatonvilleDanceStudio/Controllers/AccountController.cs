using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.ViewModels;
using EatonvilleDanceStudio.Models; // Ensure ApplicationUser namespace is included
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace EatonvilleDanceStudio.Controllers
{
    /// <summary>
    /// Provides methods for managing user accounts, including registration, authentication, and profile settings.
    /// </summary>
    [Authorize] // Restricts access to logged-in users
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Allow anonymous access for registration
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = "Zac",
                    LastName = "Kimball",
                    AccountStatus = "Approved"
                };
        
                IdentityResult result = await _userManager.CreateAsync(
                    user, 
                    model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Director");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("ManageUsers", "Director");
                }

                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        //GET
        /// <summary>
        /// Retrieves the current user's account details, such as username,
        /// email, and phone number, and renders the Manage view for updating
        /// profile information.
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns an IActionResult. If the user
        /// is found, the Manage view with pre-filled account details is rendered.
        /// If the user is not found, a NotFound result is returned with an error message.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load current user profile.");
            }

            var model = new AccountManageViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        //POST
        /// <summary>
        /// Updates the user's account information, such as phone number,
        /// and persists the changes if valid data is provided.
        /// </summary>
        /// <param name="model">An object that contains the updated account details, including phone number.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// Returns a redirect to the Manage view on successful update, or the same view with validation errors otherwise.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(AccountManageViewModel model)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load current user profile.");
            }

            // Only update if phone number changed
            if (model.PhoneNumber != user.PhoneNumber)
            {
                user.PhoneNumber = model.PhoneNumber;
                var result = await _userManager.UpdateAsync(user);
                
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            TempData["StatusMessage"] = "Your profile has been updated";
            return RedirectToAction(nameof(Manage));
        }
    }
}
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace EatonvilleDanceStudio.Controllers
{
    /// <summary>
    /// Provides methods for managing user accounts, including registration and authentication.
    /// </summary>
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

        // This endpoint displays the empty form when
        // a user navigates to /Account/Register.
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Handles the registration process for a new user.
        /// Validates the user input, creates a new user, and stores the user in the database
        /// if all validations pass, otherwise returns the view with validation errors.
        /// </summary>
        /// <param name="model">The view model containing the user registration input, including
        /// email, password, confirm password, and agreement to terms of service.</param>
        /// <returns>
        /// Returns an IActionResult. If the registration is successful, redirects to a specified
        /// route or action. If validation or creation fails, re-displays the registration page
        /// with errors.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Declare the user exactly once with all required fields
                ApplicationUser user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = "Zac",
                    LastName = "Kimball",
                    AccountStatus = "Approved"
                };
        
                // 2. Execute the database creation exactly once
                IdentityResult result = await _userManager.CreateAsync(
                    user, 
                    model.Password);

                if (result.Succeeded)
                {
                    // 3. Stamp them as a Director before signing in
                    await _userManager.AddToRoleAsync(user, "Director");
            
                    // 4. Sign them in
                    await _signInManager.SignInAsync(user, isPersistent: false);
            
                    return RedirectToAction("ManageUsers", "Director");
                }

                // If creation failed (e.g., password too weak), surface errors
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Redisplay the form with their data so they can see the errors.
            return View(model);
        }
    }
}
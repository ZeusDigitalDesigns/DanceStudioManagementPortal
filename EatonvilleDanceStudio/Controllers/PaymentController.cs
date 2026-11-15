using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.Settings;
using EatonvilleDanceStudio.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace EatonvilleDanceStudio.Controllers;

/// <summary>
/// Handles operations related to processing payments within the application.
/// </summary>
/// <remarks>
/// The PaymentController is responsible for managing payment-related actions,
/// including integration with payment providers and ensuring secure transaction processing.
/// This controller interacts with the database for payment-related data and utilizes user
/// management functionality for processing payments associated with authenticated users.
/// </remarks>
public class PaymentController(
    ApplicationDbContext context,
    IOptions<StripeSettings> stripeSettings,
    UserManager<ApplicationUser> userManager,
    IStripePaymentService paymentService)
    : Controller
{
    private readonly StripeSettings _stripeSettings = stripeSettings.Value;

    // POST

    /// <summary>
    /// Initiates a Stripe checkout session for processing a payment.
    /// </summary>
    /// <returns>
    /// A redirect to Stripe's hosted checkout page for secure payment processing.
    /// Returns a 303 status code with the location header set to the Stripe URL.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout() 
    {
        // Get the current logged in parent
        var userId = userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        
        // TEMPORARY: Hardcode the server-side amount for now. 
        // We will replace this with a real database ledger lookup in Epic 3, Issue #20.
        long amountInCents = 5000;
        
        // Get the base URL securely from settings
        var domain = _stripeSettings.Domain;
        
        // Build Stripe checkout session
        var options = new SessionCreateOptions
        {
            AllowedPaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        // Stripe expects the amount in pennies
                        UnitAmount = amountInCents,
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Monthly Tuition",
                            Description = "Secure Tuition Payment for Eatonville Dance Studio"
                        },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            Metadata = new Dictionary<string, string>
            {
                { "UserId", userId }
            },
            // Where Stripe sends user after successful payment
            SuccessUrl = domain + "/Payment/Success?session_id={CHECKOUT_SESSION_ID}",
            // Where Stripe sends user after canceled payment
            CancelUrl = domain + "/Payment/Cancel",
        };
        
        // Create the request options with the Idempotency Key to prevent duplicate charges
        var requestOptions = new RequestOptions
        {
            IdempotencyKey = $"tuition-{userId}-{DateTime.UtcNow:yyyy-MM}"
        };
        
        Session session = await paymentService.CreateSessionAsync(options, requestOptions);        
        
        // Redirect the user to Stripe's secure hosted checkout page
        Response.Headers.Add("Location", session.Url);
        return new StatusCodeResult(303);
    }
    
    // GET: /Payment/Success  **Gemini Supplied Code**

    /// <summary>
    /// Handles the completion of a payment process by validating the session ID and retrieving the payment status from Stripe.
    /// Displays appropriate success or error messages to the user and redirects to the parent dashboard.
    /// </summary>
    /// <param name="session_id">
    /// The Stripe session ID generated during the checkout process.
    /// Used to validate and retrieve the payment status from Stripe.
    /// </param>
    /// <returns>
    /// An action result that redirects to the parent dashboard.
    /// If the payment is successful, a success message is displayed.
    /// If the payment validation fails, an error message is displayed.
    /// </returns>
    
    public async Task<IActionResult> Success(string session_id)
    {
        // 1. Block immediate manual navigation without a session token
        if (string.IsNullOrEmpty(session_id))
        {
            TempData["ErrorMessage"] = "Invalid payment session.";
            return RedirectToAction("Dashboard", "Parent");
        }

        try
        {
            // 2. Fetch the authoritative session directly from Stripe's servers
            Session session = await paymentService.GetSessionAsync(session_id);

            // 3. Verify the actual payment status
            if (session.PaymentStatus == "paid")
            {
                // 4. Retrieve the parent's User ID from the metadata we embedded during Checkout
                if (session.Metadata.TryGetValue("UserId", out string parentId))
                {
                    // 5. Anti-Refresh Check: Make sure we haven't already credited this exact Stripe session
                    // (Requires: using Microsoft.EntityFrameworkCore;)
                    bool alreadyRecorded = await context.LedgerEntries
                        .AnyAsync(l => l.Description.Contains(session.Id));

                    if (!alreadyRecorded)
                    {
                        // 6. Convert Stripe's amount (cents) to dollars and make it negative for a ledger credit
                        decimal paymentAmount = (session.AmountTotal ?? 0) / 100m;
                        decimal creditAmount = -Math.Abs(paymentAmount); 

                        // 7. Create the new ledger entry
                        var paymentEntry = new LedgerEntry
                        {
                            ParentId = parentId,
                            TransactionDate = DateTime.UtcNow,
                            Description = $"Stripe Payment (Ref: {session.Id})", 
                            Amount = creditAmount,
                            Category = "Payment"
                        };

                        context.LedgerEntries.Add(paymentEntry);
                        await context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Payment successful! Your account has been credited.";
            }
            else
            {
                // Catch sessions that are pending, failed, or unpaid
                TempData["ErrorMessage"] = "Payment was not completed successfully. Please try again.";
            }
        }
        catch (StripeException)
        {
            // Catch fake or tampered session IDs that Stripe rejects
            TempData["ErrorMessage"] = "Error verifying payment with Stripe.";
        }

        return RedirectToAction("Dashboard", "Parent");
    }
    
    // GET

    /// <summary>
    /// Handles the cancellation of a payment process and informs the user that no charge was made.
    /// </summary>
    /// <returns>
    /// A redirection to the "Dashboard" action in the "Parent" controller.
    /// Returns a view where the user can see a cancellation notification.
    /// </returns>
    public IActionResult Cancel()
    {
        TempData["ErrorMessage"] = "Payment Cancelled. Your card was not charged. Please try again.";
        return RedirectToAction("Dashboard", "Parent");
    }
}
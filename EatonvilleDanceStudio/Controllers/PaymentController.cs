using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.Services;
using EatonvilleDanceStudio.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace EatonvilleDanceStudio.Controllers;

/// <summary>
/// Handles operations related to processing tuition and competition
/// fee payments via Stripe Checkout.
/// </summary>
public class PaymentController(
    ApplicationDbContext context,
    IOptions<StripeSettings> stripeSettings,
    UserManager<ApplicationUser> userManager,
    IStripePaymentService paymentService,
    INotificationService notificationService)
    : Controller
{
    private readonly StripeSettings _stripeSettings =
        stripeSettings.Value;

    // POST: /Payment/Checkout
    /// <summary>
    /// Initiates a Stripe checkout session for either a specific
    /// competition registration fee or the parent's current balance.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(
        int? competitionId = null,
        decimal? amount = null,
        string? description = null)
    {
        string? userId = userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        long amountInCents = 5000;
        string itemName = "Monthly Tuition";
        string itemDescription =
            "Secure Tuition Payment for Eatonville Dance Studio";
        string paymentCategory = "Tuition";

        // 1. Check if this payment came from a Competition Banner slide
        if (competitionId.HasValue)
        {
            Competition? comp = await context.Competitions
                .FindAsync(competitionId.Value);

            if (comp != null)
            {
                decimal feeToCharge = comp.RegistrationFee > 0
                    ? comp.RegistrationFee
                    : (amount.HasValue && amount.Value > 0
                        ? amount.Value
                        : 50.00m);

                amountInCents = (long)Math.Round(feeToCharge * 100m);
                itemName = $"{comp.Name} - Competition Fee";
                itemDescription =
                    $"Competition registration & dues for {comp.Name}";
                paymentCategory = "Competition";
            }
        }
        else
        {
            // 2. Otherwise calculate the parent's real ledger balance
            decimal currentBalance = await context.LedgerEntries
                .Where(l => l.ParentId == userId)
                .SumAsync(l => l.Amount);

            if (currentBalance > 0)
            {
                amountInCents =
                    (long)Math.Round(currentBalance * 100m);
            }
            else if (amount.HasValue && amount.Value > 0)
            {
                amountInCents =
                    (long)Math.Round(amount.Value * 100m);
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                itemName = description;
            }
        }

        string domain = _stripeSettings.Domain;

        SessionCreateOptions options = new SessionCreateOptions
        {
            AllowedPaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = amountInCents,
                        Currency = "usd",
                        ProductData =
                            new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = itemName,
                                Description = itemDescription
                            },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            Metadata = new Dictionary<string, string>
            {
                { "UserId", userId },
                { "ItemName", itemName },
                { "Category", paymentCategory },
                {
                    "CompetitionId",
                    competitionId?.ToString() ?? ""
                }
            },
            SuccessUrl =
                domain
                + "/Payment/Success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = domain + "/Payment/Cancel",
        };

        // Unique IdempotencyKey per checkout click so parents can pay
        // multiple competition fees in the same month
        RequestOptions requestOptions = new RequestOptions
        {
            IdempotencyKey =
                $"pay-{userId}-{Guid.NewGuid()}"
        };

        Session session = await paymentService.CreateSessionAsync(
            options,
            requestOptions);

        Response.Headers.Append("Location", session.Url);
        return new StatusCodeResult(303);
    }

    // GET: /Payment/Success
    /// <summary>
    /// Validates the completed Stripe session, records the ledger
    /// entry (and matching competition fee charge if applicable),
    /// and sends a confirmation receipt.
    /// </summary>
    public async Task<IActionResult> Success(string session_id)
    {
        if (string.IsNullOrEmpty(session_id))
        {
            TempData["ErrorMessage"] = "Invalid payment session.";
            return RedirectToAction("Dashboard", "Parent");
        }

        try
        {
            Session session =
                await paymentService.GetSessionAsync(session_id);

            if (session.PaymentStatus == "paid")
            {
                if (session.Metadata.TryGetValue(
                    "UserId",
                    out string? parentId)
                    && !string.IsNullOrEmpty(parentId))
                {
                    bool alreadyRecorded = await context.LedgerEntries
                        .AnyAsync(l =>
                            l.Description.Contains(session.Id));

                    if (!alreadyRecorded)
                    {
                        decimal paymentAmount =
                            (session.AmountTotal ?? 0) / 100m;
                        decimal creditAmount =
                            -Math.Abs(paymentAmount);

                        string itemName =
                            session.Metadata.TryGetValue(
                                "ItemName",
                                out string? metaItem)
                            && !string.IsNullOrWhiteSpace(metaItem)
                                ? metaItem
                                : "Stripe Payment";

                        string category =
                            session.Metadata.TryGetValue(
                                "Category",
                                out string? metaCategory)
                            && !string.IsNullOrWhiteSpace(metaCategory)
                                ? metaCategory
                                : "Payment";

                        // If paying a Competition Fee directly from
                        // the banner, record the fee charge on the
                        // ledger too so the parent's tuition balance
                        // stays accurate
                        if (category == "Competition")
                        {
                            LedgerEntry compCharge = new LedgerEntry
                            {
                                ParentId = parentId,
                                TransactionDate = DateTime.UtcNow,
                                Description = itemName,
                                Amount = Math.Abs(paymentAmount),
                                Category = "Competition"
                            };
                            context.LedgerEntries.Add(compCharge);
                        }

                        LedgerEntry paymentEntry = new LedgerEntry
                        {
                            ParentId = parentId,
                            TransactionDate = DateTime.UtcNow,
                            Description =
                                $"{itemName} (Ref: {session.Id})",
                            Amount = creditAmount,
                            Category = "Payment"
                        };

                        context.LedgerEntries.Add(paymentEntry);
                        await context.SaveChangesAsync();

                        ParentProfile? parentProfile =
                            await context.ParentProfiles
                                .Include(p => p.User)
                                .FirstOrDefaultAsync(
                                    p => p.UserId == parentId);

                        if (parentProfile != null)
                        {
                            string email =
                                parentProfile.User?.Email ?? "";
                            await notificationService
                                .SendPaymentConfirmationAsync(
                                    email,
                                    parentProfile.PrimaryPhone,
                                    parentProfile.FirstName,
                                    paymentAmount);
                        }
                    }
                }

                TempData["SuccessMessage"] =
                    "Payment successful! Your account has been " +
                    "credited.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Payment was not completed successfully. " +
                    "Please try again.";
            }
        }
        catch (StripeException)
        {
            TempData["ErrorMessage"] =
                "Error verifying payment with Stripe.";
        }

        return RedirectToAction("Dashboard", "Parent");
    }

    // GET: /Payment/Cancel
    /// <summary>
    /// Handles a canceled Stripe checkout session and redirects back
    /// to the Parent Dashboard.
    /// </summary>
    public IActionResult Cancel()
    {
        TempData["ErrorMessage"] =
            "Payment Cancelled. Your card was not charged.";
        return RedirectToAction("Dashboard", "Parent");
    }
}
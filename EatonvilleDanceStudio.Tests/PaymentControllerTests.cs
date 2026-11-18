using System.Collections.Generic;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Controllers;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.Services;
using EatonvilleDanceStudio.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Stripe.Checkout;
using Xunit;

namespace EatonvilleDanceStudio.Tests;

/// <summary>
/// Unit tests verifying Stripe competition fee checkout pricing and
/// idempotent ledger entry recording on payment success (#41).
/// </summary>
public class PaymentControllerTests
{
    [Fact]
    public async Task Success_CompetitionPayment_RecordsChargeAndCreditOnce()
    {
        // Arrange
        using ApplicationDbContext context =
            TestDbContextFactory.Create();
        string parentUserId = "parent-stripe-1";

        Mock<IUserStore<ApplicationUser>> store =
            new Mock<IUserStore<ApplicationUser>>();
        Mock<UserManager<ApplicationUser>> mockUserMgr =
            new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

        IOptions<StripeSettings> stripeOpts = Options.Create(
            new StripeSettings
            {
                Domain = "https://localhost:5001",
                SecretKey = "sk_test_mock",
                PublishableKey = "pk_test_mock"
            });

        Mock<IStripePaymentService> mockStripe =
            new Mock<IStripePaymentService>();
        Mock<INotificationService> mockNotify =
            new Mock<INotificationService>();

        Session paidSession = new Session
        {
            Id = "cs_test_comp_999",
            PaymentStatus = "paid",
            AmountTotal = 15000, // $150.00 in cents
            Metadata = new Dictionary<string, string>
            {
                { "UserId", parentUserId },
                { "ItemName", "Starlight - Competition Fee" },
                { "Category", "Competition" },
                { "CompetitionId", "5" }
            }
        };

        mockStripe
            .Setup(s => s.GetSessionAsync("cs_test_comp_999"))
            .ReturnsAsync(paidSession);

        PaymentController controller = new PaymentController(
            context,
            stripeOpts,
            mockUserMgr.Object,
            mockStripe.Object,
            mockNotify.Object)
        {
            TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>())
        };

        // Act: Call Success twice to test anti-refresh protection
        await controller.Success("cs_test_comp_999");
        await controller.Success("cs_test_comp_999");

        // Assert: Exactly 2 entries (1 Comp charge +150, 1 Payment -150)
        // and net balance change is $0.00
        List<LedgerEntry> entries = await context.LedgerEntries
            .ToListAsync();
        Assert.Equal(2, entries.Count);

        decimal netBalance = await context.LedgerEntries
            .SumAsync(l => l.Amount);
        Assert.Equal(0m, netBalance);
    }
}
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Controllers;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace EatonvilleDanceStudio.Tests;

/// <summary>
/// Unit tests verifying Parent Dashboard ledger balance math,
/// household data isolation, dancer age calculation, and competition
/// entry attendance toggling (#41).
/// </summary>
public class EnrollmentAndDashboardTests
{
    private static Mock<UserManager<ApplicationUser>>
        CreateMockUserManager(ApplicationUser appUser)
    {
        Mock<IUserStore<ApplicationUser>> store =
            new Mock<IUserStore<ApplicationUser>>();
        Mock<UserManager<ApplicationUser>> mgr =
            new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

        mgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(appUser.Id);

        mgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(appUser);

        return mgr;
    }

    [Fact]
    public async Task Dashboard_CalculatesNetBalanceAndFiltersHousehold()
    {
        // Arrange
        using ApplicationDbContext context =
            TestDbContextFactory.Create();
        string userId = "parent-dash-1";

        ApplicationUser appUser = new ApplicationUser
        {
            Id = userId,
            UserName = "zac@example.com",
            Email = "zac@example.com",
            FirstName = "Zac",
            LastName = "Kimball"
        };

        ParentProfile profile = new ParentProfile
        {
            Id = 1,
            UserId = userId,
            User = appUser,
            FirstName = "Zac",
            LastName = "Kimball",
            PrimaryPhone = "555-1234",
            EmergencyContactName = "Heather Kimball",
            EmergencyContactPhone = "555-5678",
            AccountStatus = "Approved"
        };

        Dancer dancer = new Dancer
        {
            Id = 10,
            FirstName = "Zoey",
            LastName = "Kimball",
            DateOfBirth = new DateTime(2015, 2, 27),
            SkillLevel = "Intermediate",
            ParentProfileId = profile.Id
        };

        DanceClass jazzClass = new DanceClass
        {
            Id = 5,
            ClassName = "Jazz II",
            DayOfWeek = "Tuesday"
        };

        CostumeOrder costume = new CostumeOrder
        {
            Id = 50,
            DancerId = dancer.Id,
            DanceClassId = jazzClass.Id,
            CostumeName = "Gold Sequins",
            OrderStatus = "Ordered",
            PaymentStatus = "Paid"
        };

        // Add +$200 charge and -$75 payment => Net balance = $125.00
        LedgerEntry tuitionCharge = new LedgerEntry
        {
            ParentId = userId,
            TransactionDate = DateTime.UtcNow.AddDays(-5),
            Description = "Monthly Tuition",
            Amount = 200.00m,
            Category = "Tuition"
        };

        LedgerEntry partialPayment = new LedgerEntry
        {
            ParentId = userId,
            TransactionDate = DateTime.UtcNow.AddDays(-1),
            Description = "Stripe Payment",
            Amount = -75.00m,
            Category = "Payment"
        };

        LegalWaiver signedWaiver = new LegalWaiver
        {
            ParentProfileId = profile.Id,
            Season = "2026-2027"
        };
        
        context.Users.Add(appUser);
        context.ParentProfiles.Add(profile);
        context.LegalWaivers.Add(signedWaiver);
        context.Dancers.Add(dancer);
        context.DanceClasses.Add(jazzClass);
        context.CostumeOrders.Add(costume);
        context.LedgerEntries.AddRange(tuitionCharge, partialPayment);
        await context.SaveChangesAsync();

        Mock<UserManager<ApplicationUser>> mockUserMgr =
            CreateMockUserManager(appUser);

        DefaultHttpContext httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        ParentController controller =
            new ParentController(context, mockUserMgr.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },
                TempData = new TempDataDictionary(
                    httpContext,
                    Mock.Of<ITempDataProvider>())
            };

        // Act
        IActionResult result = await controller.Dashboard();

        // Assert
        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        ParentDashboardViewModel model =
            Assert.IsType<ParentDashboardViewModel>(viewResult.Model);

        Assert.Equal(125.00m, model.CurrentBalance);
        Assert.Single(model.Dancers);
        Assert.Single(model.CostumeOrders);
        Assert.Single(model.RecentCharges);
        Assert.Single(model.RecentPayments);
    }

    [Fact]
    public void Dancer_Age_CalculatesCorrectlyFromDateOfBirth()
    {
        // Arrange
        DateTime tenYearsAgo = DateTime.Today.AddYears(-10);
        Dancer dancer = new Dancer
        {
            FirstName = "Zoey",
            LastName = "Kimball",
            DateOfBirth = tenYearsAgo,
            SkillLevel = "Intermediate"
        };

        // Act & Assert
        Assert.Equal(10, dancer.Age);
    }

    [Fact]
    public async Task ToggleAttendance_FlipsCompetitionStudioStatus()
    {
        // Arrange
        using ApplicationDbContext context =
            TestDbContextFactory.Create();
        Mock<IWebHostEnvironment> mockEnv =
            new Mock<IWebHostEnvironment>();

        Competition comp = new Competition
        {
            Id = 1,
            Name = "Starlight Nationals",
            StartDate = DateTime.Today.AddDays(30),
            IsStudioAttending = false
        };

        context.Competitions.Add(comp);
        await context.SaveChangesAsync();

        CompetitionsController controller =
            new CompetitionsController(context, mockEnv.Object);

        // Act
        await controller.ToggleAttendance(comp.Id, true);

        // Assert
        Competition? updated =
            await context.Competitions.FindAsync(comp.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsStudioAttending);
    }
}
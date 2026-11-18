using System.Security.Claims;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Controllers;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace EatonvilleDanceStudio.Tests;

/// <summary>
/// Unit tests verifying ParentController costume order status updates
/// and household ownership security (#80 & #41).
/// </summary>
public class ParentCostumeStatusTests
{
    private static Mock<UserManager<ApplicationUser>>
        CreateMockUserManager(string userId)
    {
        Mock<IUserStore<ApplicationUser>> store =
            new Mock<IUserStore<ApplicationUser>>();
        Mock<UserManager<ApplicationUser>> mgr =
            new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

        mgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        return mgr;
    }

    private static ParentController CreateController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        return new ParentController(context, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task UpdateCostumeStatus_Ordered_SetsBindingConfirmed()
    {
        // Arrange
        using ApplicationDbContext context =
            TestDbContextFactory.Create();
        string parentUserId = "parent-user-1";

        ApplicationUser parentUser = new ApplicationUser
        {
            Id = parentUserId,
            UserName = "heather@example.com",
            Email = "heather@example.com",
            FirstName = "Heather",
            LastName = "Kimball"
        };

        ParentProfile profile = new ParentProfile
        {
            Id = 1,
            UserId = parentUserId,
            User = parentUser,
            FirstName = "Heather",
            LastName = "Kimball",
            PrimaryPhone = "555-0100",
            EmergencyContactName = "Zac Kimball",
            EmergencyContactPhone = "555-0101",
            AccountStatus = "Approved"
        };

        Dancer dancer = new Dancer
        {
            Id = 10,
            FirstName = "Zoey",
            LastName = "Kimball",
            DateOfBirth = new System.DateTime(2015, 2, 27),
            SkillLevel = "Intermediate",
            ParentProfileId = profile.Id
        };

        CostumeOrder order = new CostumeOrder
        {
            Id = 100,
            DancerId = dancer.Id,
            CostumeName = "Neon Jazz Sequins",
            OrderStatus = "Ordered",
            PaymentStatus = "Paid",
            IsBindingConfirmed = true
        };

        context.Users.Add(parentUser);
        context.ParentProfiles.Add(profile);
        context.Dancers.Add(dancer);
        context.CostumeOrders.Add(order);
        await context.SaveChangesAsync();

        Mock<UserManager<ApplicationUser>> mockUserMgr =
            CreateMockUserManager(parentUserId);

        ParentController controller =
            CreateController(context, mockUserMgr.Object);

        // Act
        IActionResult result =
            await controller.UpdateCostumeStatus(100, "Ordered");

        // Assert
        RedirectToActionResult redirect =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirect.ActionName);

        CostumeOrder? updated =
            await context.CostumeOrders.FindAsync(100);
        Assert.NotNull(updated);
        Assert.Equal("Ordered", updated.OrderStatus);
        Assert.True(updated.IsBindingConfirmed);
    }

    [Fact]
    public async Task UpdateCostumeStatus_BlocksOtherHouseholdOrder()
    {
        // Arrange
        using ApplicationDbContext context =
            TestDbContextFactory.Create();

        ApplicationUser userOne = new ApplicationUser
        {
            Id = "parent-1",
            UserName = "parent1@example.com",
            Email = "parent1@example.com",
            FirstName = "Parent",
            LastName = "One"
        };

        ApplicationUser userTwo = new ApplicationUser
        {
            Id = "parent-2",
            UserName = "parent2@example.com",
            Email = "parent2@example.com",
            FirstName = "Parent",
            LastName = "Two"
        };

        ParentProfile loggedInParent = new ParentProfile
        {
            Id = 1,
            UserId = "parent-1",
            User = userOne,
            FirstName = "Parent",
            LastName = "One",
            PrimaryPhone = "555-0101",
            EmergencyContactName = "Contact One",
            EmergencyContactPhone = "555-0102",
            AccountStatus = "Approved"
        };

        ParentProfile otherParent = new ParentProfile
        {
            Id = 2,
            UserId = "parent-2",
            User = userTwo,
            FirstName = "Parent",
            LastName = "Two",
            PrimaryPhone = "555-0201",
            EmergencyContactName = "Contact Two",
            EmergencyContactPhone = "555-0202",
            AccountStatus = "Approved"
        };

        Dancer otherDancer = new Dancer
        {
            Id = 20,
            FirstName = "Other",
            LastName = "Dancer",
            DateOfBirth = new System.DateTime(2016, 5, 10),
            SkillLevel = "Beginner",
            ParentProfileId = otherParent.Id
        };

        CostumeOrder otherOrder = new CostumeOrder
        {
            Id = 200,
            DancerId = otherDancer.Id,
            CostumeName = "Ballet Tutu",
            OrderStatus = "Not Ordered",
            PaymentStatus = "Unpaid"
        };

        context.Users.AddRange(userOne, userTwo);
        context.ParentProfiles.AddRange(loggedInParent, otherParent);
        context.Dancers.Add(otherDancer);
        context.CostumeOrders.Add(otherOrder);
        await context.SaveChangesAsync();

        Mock<UserManager<ApplicationUser>> mockUserMgr =
            CreateMockUserManager("parent-1");

        ParentController controller =
            CreateController(context, mockUserMgr.Object);

        // Act: parent-1 tries to change parent-2's costume order
        await controller.UpdateCostumeStatus(200, "Received");

        // Assert: Status must remain untouched
        CostumeOrder? unchanged =
            await context.CostumeOrders.FindAsync(200);
        Assert.NotNull(unchanged);
        Assert.Equal("Not Ordered", unchanged.OrderStatus);
    }
}
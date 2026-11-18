using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Controllers;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewComponents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EatonvilleDanceStudio.Tests;

/// <summary>
/// Unit tests verifying competition creation defaults, fee due dates,
/// featured banner toggles, and the CompetitionBannersViewComponent.
/// </summary>
public class CompetitionsAndBannersTests
{
    [Fact]
    public async Task Create_SetsDefaultHexAndTwoWeekPriorDueDate()
    {
        // Arrange
        using ApplicationDbContext context =
            TestDbContextFactory.Create();
        Mock<IWebHostEnvironment> mockEnv =
            new Mock<IWebHostEnvironment>();

        CompetitionsController controller =
            new CompetitionsController(context, mockEnv.Object);

        DateTime compStart = new DateTime(2027, 3, 15);
        Competition newComp = new Competition
        {
            Name = "Spotlight Regional",
            EventFormat = "Competition",
            StartDate = compStart,
            RegistrationFee = 125.00m,
            BrandColorHex = "",
            FeeDueDate = null
        };

        // Act
        IActionResult result =
            await controller.Create(newComp, null);

        // Assert
        RedirectToActionResult redirect =
            Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);

        Competition? saved = await context.Competitions
            .FirstOrDefaultAsync(c => c.Name == "Spotlight Regional");

        Assert.NotNull(saved);
        Assert.Equal("#ff10f0", saved.BrandColorHex);
        Assert.Equal(compStart.AddDays(-14), saved.FeeDueDate);
        Assert.Equal(125.00m, saved.RegistrationFee);
    }

    [Fact]
    public async Task ToggleFeatured_FlipsIsFeaturedAnnouncement()
    {
        // Arrange
        using ApplicationDbContext context =
            TestDbContextFactory.Create();
        Mock<IWebHostEnvironment> mockEnv =
            new Mock<IWebHostEnvironment>();

        Competition comp = new Competition
        {
            Name = "Rainbow Dance",
            StartDate = DateTime.Today.AddDays(30),
            IsFeaturedAnnouncement = false
        };
        context.Competitions.Add(comp);
        await context.SaveChangesAsync();

        CompetitionsController controller =
            new CompetitionsController(context, mockEnv.Object);

        // Act
        await controller.ToggleFeatured(comp.Id, true);

        // Assert
        Competition? updated =
            await context.Competitions.FindAsync(comp.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsFeaturedAnnouncement);
    }

    [Fact]
    public async Task BannerViewComponent_ReturnsOnlyFeaturedUpcoming()
    {
        // Arrange
        using ApplicationDbContext context =
            TestDbContextFactory.Create();

        context.Competitions.AddRange(
            new Competition
            {
                Name = "Featured Upcoming",
                StartDate = DateTime.Today.AddDays(14),
                IsFeaturedAnnouncement = true
            },
            new Competition
            {
                Name = "Hidden Upcoming",
                StartDate = DateTime.Today.AddDays(14),
                IsFeaturedAnnouncement = false
            },
            new Competition
            {
                Name = "Featured Expired",
                StartDate = DateTime.Today.AddDays(-20),
                EndDate = DateTime.Today.AddDays(-18),
                IsFeaturedAnnouncement = true
            });
        await context.SaveChangesAsync();

        CompetitionBannersViewComponent component =
            new CompetitionBannersViewComponent(context);

        // Act
        IViewComponentResult result = await component.InvokeAsync();

        // Assert
        ViewViewComponentResult viewResult =
            Assert.IsType<ViewViewComponentResult>(result);
        List<Competition> model = Assert.IsAssignableFrom<
            List<Competition>>(viewResult.ViewData!.Model);

        Assert.Single(model);
        Assert.Equal("Featured Upcoming", model[0].Name);
    }
}
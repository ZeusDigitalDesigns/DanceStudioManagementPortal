using EatonvilleDanceStudio.Controllers;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace EatonvilleDanceStudio.Tests;

public class TestWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = "/tmp";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ApplicationName { get; set; } = "EatonvilleDanceStudio";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = "/tmp";
    public string EnvironmentName { get; set; } = "Development";
}

public class VideoVaultControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public VideoVaultControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _env = new TestWebHostEnvironment();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Index_ReturnsVideos_OrderedByUpLoadedAtDescending()
    {
        // Arrange
        var danceClass = new DanceClass
        {
            ClassName = "Ballet 1",
            RequiredSkillLevel = "Beginner",
            DayOfWeek = "Monday",
            StartTime = new TimeSpan(17, 0, 0),
            EndTime = new TimeSpan(18, 0, 0),
            MinimumAge = 5,
            MaximumAge = 8,
            Capacity = 10,
            MonthlyCost = 50.00m
        };
        _context.DanceClasses.Add(danceClass);
        await _context.SaveChangesAsync();

        var video1 = new RehearsalVideo
        {
            Title = "Old Video",
            DanceClassId = danceClass.Id,
            VideoUrl = "https://example.com/1",
            UpLoadedAt = DateTime.UtcNow.AddDays(-2)
        };
        var video2 = new RehearsalVideo
        {
            Title = "New Video",
            DanceClassId = danceClass.Id,
            VideoUrl = "https://example.com/2",
            UpLoadedAt = DateTime.UtcNow
        };

        _context.RehearsalVideos.AddRange(video1, video2);
        await _context.SaveChangesAsync();

        var controller = new VideoVaultController(_context, _env);

        // Act
        var result = await controller.Index(null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<RehearsalVideo>>(viewResult.Model);
        Assert.Equal(2, model.Count);
        Assert.Equal("New Video", model[0].Title);
        Assert.Equal("Old Video", model[1].Title);
    }

    [Fact]
    public async Task Create_Post_AddsVideoWithUpLoadedAt()
    {
        // Arrange
        var danceClass = new DanceClass
        {
            ClassName = "Tap 1",
            RequiredSkillLevel = "Beginner",
            DayOfWeek = "Tuesday",
            StartTime = new TimeSpan(18, 0, 0),
            EndTime = new TimeSpan(19, 0, 0),
            MinimumAge = 6,
            MaximumAge = 9,
            Capacity = 12,
            MonthlyCost = 55.00m
        };
        _context.DanceClasses.Add(danceClass);
        await _context.SaveChangesAsync();

        var controller = new VideoVaultController(_context, _env);
        var uploadModel = new RehearsalVideoUploadViewModel
        {
            Title = "Choreography Practice",
            Description = "Practice routine",
            DanceClassId = danceClass.Id,
            ExternalOrLocalUrl = "https://example.com/practice"
        };

        // Act
        var result = await controller.Create(uploadModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(VideoVaultController.Index), redirectResult.ActionName);

        var savedVideo = await _context.RehearsalVideos.FirstOrDefaultAsync(v => v.Title == "Choreography Practice");
        Assert.NotNull(savedVideo);
        Assert.NotNull(savedVideo.UpLoadedAt);
        Assert.Equal(danceClass.Id, savedVideo.DanceClassId);
    }
}

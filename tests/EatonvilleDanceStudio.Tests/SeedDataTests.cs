using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EatonvilleDanceStudio.Tests;

public class SeedDataTests
{
    [Fact]
    public async Task InitializeAsync_SeedsRolesUsersProfilesAndClassesSuccessfully()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connection));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        var serviceProvider = services.BuildServiceProvider();

        // Create schema
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
        }

        // Act
        await SeedData.InitializeAsync(serviceProvider);

        // Assert
        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Check roles
        Assert.True(await roleManager.RoleExistsAsync("Director"));
        Assert.True(await roleManager.RoleExistsAsync("Instructor"));
        Assert.True(await roleManager.RoleExistsAsync("Parent"));
        Assert.True(await roleManager.RoleExistsAsync("Dancer"));

        // Check Director
        var director = await userManager.FindByEmailAsync("director@gmail.com");
        Assert.NotNull(director);
        Assert.True(await userManager.IsInRoleAsync(director, "Director"));

        // Check Instructor
        var instructor = await userManager.FindByEmailAsync("instructor@gmail.com");
        Assert.NotNull(instructor);
        Assert.True(await userManager.IsInRoleAsync(instructor, "Instructor"));

        // Check Parent
        var parent = await userManager.FindByEmailAsync("parent@gmail.com");
        Assert.NotNull(parent);
        Assert.True(await userManager.IsInRoleAsync(parent, "Parent"));

        // Check ParentProfile
        var parentProfile = await db.ParentProfiles.FirstOrDefaultAsync(p => p.UserId == parent.Id);
        Assert.NotNull(parentProfile);
        Assert.Equal("Test", parentProfile.FirstName);
        Assert.Equal("Parent", parentProfile.LastName);

        // Check Dancer
        var dancer = await db.Dancers.FirstOrDefaultAsync(d => d.ParentProfileId == parentProfile.Id);
        Assert.NotNull(dancer);
        Assert.Equal("Little", dancer.FirstName);

        // Check LedgerEntry
        var ledger = await db.LedgerEntries.FirstOrDefaultAsync(l => l.ParentId == parent.Id);
        Assert.NotNull(ledger);
        Assert.Equal(150.00m, ledger.Amount);

        // Check DanceClasses
        var classes = await db.DanceClasses.ToListAsync();
        Assert.Equal(2, classes.Count);
        Assert.All(classes, c => Assert.Equal(instructor.Id, c.InstructorId));

        // Act again to ensure idempotency
        await SeedData.InitializeAsync(serviceProvider);
        Assert.Equal(2, await db.DanceClasses.CountAsync());
    }
}

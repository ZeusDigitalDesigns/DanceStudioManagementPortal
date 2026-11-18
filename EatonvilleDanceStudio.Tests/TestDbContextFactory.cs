using System;
using EatonvilleDanceStudio.Data;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Tests;

/// <summary>
/// Creates isolated in-memory ApplicationDbContext instances for
/// unit testing controllers and ViewComponents (#41).
/// </summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString())
                .Options;

        ApplicationDbContext context =
            new ApplicationDbContext(options);

        context.Database.EnsureCreated();
        return context;
    }
}
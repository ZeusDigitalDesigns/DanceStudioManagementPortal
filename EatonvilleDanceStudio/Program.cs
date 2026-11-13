using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.Settings;
using EatonvilleDanceStudio.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string? connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
    
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
    
builder.Services.AddControllersWithViews();

builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();

StripeConfiguration.ApiKey = builder.Configuration
    .GetSection("Stripe")["SecretKey"];

builder.Services.AddScoped<ITuitionCalculatorService,
    TuitionCalculatorService>();

// Summary: Increases Kestrel and multipart form upload limits to
// 500 MB so formatted rehearsal videos do not fail on POST.
builder.WebHost.ConfigureKestrel((KestrelServerOptions options) =>
{
    options.Limits.MaxRequestBodySize = 524_288_000;
});

builder.Services.Configure<FormOptions>((FormOptions options) =>
{
    options.MultipartBodyLengthLimit = 524_288_000;
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// Handle Migrations and Seeding
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    try
    {
        ApplicationDbContext context = services
            .GetRequiredService<ApplicationDbContext>();
            
        // Apply any pending Entity Framework Migrations
        context.Database.Migrate();

        // Run your custom Seeders (Test Instructor is now in SeedData)
        RoleManager<IdentityRole> roleManager = services
            .GetRequiredService<RoleManager<IdentityRole>>();
            
        await ContextSeed.SeedRolesAsync(roleManager);
        await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        ILogger<Program> logger = services
            .GetRequiredService<ILogger<Program>>();
            
        logger.LogCritical(ex, "Database migration or seeding failed.");
        throw; // Intentionally crash if the DB fails to build
    }
}

app.Run();
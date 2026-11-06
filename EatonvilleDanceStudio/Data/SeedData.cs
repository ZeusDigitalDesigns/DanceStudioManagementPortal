using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.Data;

/// <summary>
/// Provides functionality for seeding initial data into the application's 
/// database during startup. This includes setting up foundational roles, 
/// creating a master account, provisioning a test instructor, and 
/// assigning starter dance classes.
/// </summary>
public class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // Resolve managers and DB Context from dependency injection
        RoleManager<IdentityRole> roleManager = serviceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        UserManager<ApplicationUser> userManager = serviceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        ApplicationDbContext context = serviceProvider
            .GetRequiredService<ApplicationDbContext>();

        // Define and create the foundational roles for the studio
        string[] roleNames = { "Director", "Instructor", "Parent", "Dancer" };

        foreach (string roleName in roleNames)
        {
            bool roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Define and create the master Director account
        string directorEmail = "director@gmail.com";

        ApplicationUser? directorUser = await userManager
            .FindByEmailAsync(directorEmail);

        if (directorUser == null)
        {
            directorUser = new ApplicationUser
            {
                UserName = directorEmail,
                Email = directorEmail,
                EmailConfirmed = true,
                FirstName = "Studio",
                LastName = "Director"
            };

            IdentityResult createPowerUser = await userManager
                .CreateAsync(directorUser, "NewPassword123!");

            if (createPowerUser.Succeeded)
            {
                await userManager.AddToRoleAsync(directorUser, "Director");
            }
            else
            {
                foreach (IdentityError error in createPowerUser.Errors)
                {
                    Console.WriteLine($"Failed to create Director: " +
                                      $"{error.Description}");
                }
            }
        }
        else
        {
            if (!directorUser.EmailConfirmed)
            {
                directorUser.EmailConfirmed = true;
                await userManager.UpdateAsync(directorUser);
            }

            if (!await userManager.IsInRoleAsync(directorUser, "Director"))
            {
                await userManager.AddToRoleAsync(directorUser, "Director");
            }
        }

        // Define and create the Test Instructor account
        string instructorEmail = "instructor@gmail.com";

        ApplicationUser? instructorUser = await userManager
            .FindByEmailAsync(instructorEmail);

        if (instructorUser == null)
        {
            instructorUser = new ApplicationUser
            {
                UserName = instructorEmail,
                Email = instructorEmail,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Instructor"
            };

            IdentityResult createInstructor = await userManager
                .CreateAsync(instructorUser, "Password123!");

            if (createInstructor.Succeeded)
            {
                await userManager.AddToRoleAsync(instructorUser, "Instructor");
            }
        }
        else
        {
            if (!instructorUser.EmailConfirmed)
            {
                instructorUser.EmailConfirmed = true;
                await userManager.UpdateAsync(instructorUser);
            }

            if (!await userManager.IsInRoleAsync(instructorUser, "Instructor"))
            {
                await userManager.AddToRoleAsync(instructorUser, "Instructor");
            }
        }

        // 1. Ensure Parent Role Exists
        if (!await roleManager.RoleExistsAsync("Parent"))
        {
            await roleManager.CreateAsync(new IdentityRole("Parent"));
        }

        // 2. Create the Parent User
        var parentEmail = "parent@gmail.com";
        var parentUser = await userManager.FindByEmailAsync(parentEmail);
        if (parentUser == null)
        {
            parentUser = new ApplicationUser
            {
                UserName = parentEmail,
                Email = parentEmail,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Parent"
            };
            var createParent = await userManager.CreateAsync(parentUser, "Password123!");
            if (createParent.Succeeded)
            {
                await userManager.AddToRoleAsync(parentUser, "Parent");
            }
        }
        else
        {
            if (!parentUser.EmailConfirmed)
            {
                parentUser.EmailConfirmed = true;
                await userManager.UpdateAsync(parentUser);
            }

            if (!await userManager.IsInRoleAsync(parentUser, "Parent"))
            {
                await userManager.AddToRoleAsync(parentUser, "Parent");
            }
        }

        if (parentUser != null)
        {
            // 3. Create the ParentProfile
            var parentProfile = await context.ParentProfiles.FirstOrDefaultAsync(p => p.UserId == parentUser.Id);
            if (parentProfile == null)
            {
                parentProfile = new ParentProfile
                {
                    UserId = parentUser.Id,
                    User = parentUser,
                    FirstName = parentUser.FirstName,
                    LastName = parentUser.LastName,
                    PrimaryPhone = "555-0100",
                    EmergencyContactName = "Jane Doe",
                    EmergencyContactPhone = "555-0199",
                    AccountStatus = "Approved"
                };
                context.ParentProfiles.Add(parentProfile);
                await context.SaveChangesAsync();
            }

            // 4. Create a test Dancer and a Ledger Entry so the dashboard has data
            if (!context.Dancers.Any(d => d.ParentProfileId == parentProfile.Id))
            {
                var testDancer = new Dancer
                {
                    FirstName = "Little",
                    LastName = "Dancer",
                    DateOfBirth = new DateTime(2015, 5, 10), // 11 years old
                    SkillLevel = "Beginner",
                    ParentProfileId = parentProfile.Id,
                    Parent = parentProfile,
                    PhotoApprovalStatus = "Approved",
                    AccountStatus = "Approved"
                };
                context.Dancers.Add(testDancer);
        
                // Seed an outstanding tuition charge so the balance isn't $0.00
                context.LedgerEntries.Add(new LedgerEntry
                {
                    ParentId = parentUser.Id,
                    TransactionDate = DateTime.Now.AddDays(-2),
                    Description = "November Tuition",
                    Amount = 150.00m,
                    Category = "Tuition"
                });
        
                await context.SaveChangesAsync();
            }
        }
        
        // Ensure the "Dancer" role exists
        if (!await roleManager.RoleExistsAsync("Dancer"))
        {
            IdentityRole dancerRole = new IdentityRole("Dancer");
            await roleManager.CreateAsync(dancerRole);
        }

        // Check if the Dancer login already exists
        ApplicationUser? dancerUser = 
            await userManager.FindByEmailAsync("zoey@gmail.com");

        if (dancerUser == null)
        {
            // Create the Identity User for the login
            dancerUser = new ApplicationUser
            {
                UserName = "zoey@gmail.com",
                Email = "zoey@gmail.com",
                FirstName = "Zoey",
                LastName = "Kimball",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(dancerUser, "Password123!");
            await userManager.AddToRoleAsync(dancerUser, "Dancer");

            // Fetch the seeded Parent to link the foreign key
            // *Update this email if your seeded parent uses a different one!*
            parentUser = await userManager.FindByEmailAsync("parent@gmail.com");
        
            ParentProfile? parentProfile = null;
            if (parentUser != null)
            {
                parentProfile = await context.ParentProfiles
                    .FirstOrDefaultAsync(p => p.UserId == parentUser.Id);
            }

            // Create the Dancer profile and link both the User and Parent
            if (parentProfile != null)
            {
                Dancer seededDancer = new Dancer
                {
                    FirstName = "Zoey",
                    LastName = "Kimball",
                    DateOfBirth = new DateTime(2015, 2, 27),
                    UserId = dancerUser.Id,            // Links the login
                    ParentProfileId = parentProfile.Id // Links the family
                };

                context.Dancers.Add(seededDancer);
                await context.SaveChangesAsync();
            }
        }

        // Provision starter Dance Classes linked to the Test Instructor
        if (instructorUser != null)
        {
            if (!context.DanceClasses.Any())
            {
                List<DanceClass> starterClasses = new List<DanceClass>
                {
                    new DanceClass
                    {
                        ClassName = "Beginner Ballet",
                        Description = "A fun, introductory ballet class " +
                                      "focusing on basic movement and rhythm.",
                        MinimumAge = 3,
                        MaximumAge = 5,
                        RequiredSkillLevel = "Beginner",
                        Capacity = 12,
                        DayOfWeek = "Monday",
                        StartTime = new TimeSpan(15, 30, 0), // 3:30 PM
                        EndTime = new TimeSpan(16, 15, 0),   // 4:15 PM
                        InstructorId = instructorUser.Id     // Link to Instructor
                    },
                    new DanceClass
                    {
                        ClassName = "Intermediate Hip Hop",
                        Description = "High-energy hip hop routine focusing " +
                                      "on choreography and stage presence.",
                        MinimumAge = 10,
                        MaximumAge = 15,
                        RequiredSkillLevel = "Intermediate",
                        Capacity = 20,
                        DayOfWeek = "Wednesday",
                        StartTime = new TimeSpan(17, 0, 0), // 5:00 PM
                        EndTime = new TimeSpan(18, 0, 0),   // 6:00 PM
                        InstructorId = instructorUser.Id    // Link to Instructor
                    }
                };

                context.DanceClasses.AddRange(starterClasses);
                await context.SaveChangesAsync();
            }
            else
            {
                // Fallback: If classes exist but have no instructor, assign them
                List<DanceClass> orphanedClasses = await context.DanceClasses
                    .Where(c => string.IsNullOrEmpty(c.InstructorId))
                    .ToListAsync();

                if (orphanedClasses.Any())
                {
                    foreach (DanceClass orphanedClass in orphanedClasses)
                    {
                        orphanedClass.InstructorId = instructorUser.Id;
                    }

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
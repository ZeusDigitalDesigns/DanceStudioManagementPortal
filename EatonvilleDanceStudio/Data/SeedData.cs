using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
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

        // Provision starter Dance Classes linked to the Test Instructor
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
                    InstructorId = instructorUser!.Id    // Link to Instructor
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
                    StartTime = new TimeSpan(17, 0, 0),  // 5:00 PM
                    EndTime = new TimeSpan(18, 0, 0),    // 6:00 PM
                    InstructorId = instructorUser!.Id    // Link to Instructor
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
                    orphanedClass.InstructorId = instructorUser!.Id;
                }
                await context.SaveChangesAsync();
            }
        }
    } 
}
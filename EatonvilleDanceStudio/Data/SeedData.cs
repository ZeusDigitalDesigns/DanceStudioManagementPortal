using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.Data;

/// <summary>
/// Provides functionality for seeding initial data into the application's 
/// database during startup. This includes setting up foundational roles, 
/// creating a master account, provisioning a test instructor, assigning 
/// starter dance classes, and loading the competition schedule.
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

        // Ensure Parent Role Exists
        if (!await roleManager.RoleExistsAsync("Parent"))
        {
            await roleManager.CreateAsync(new IdentityRole("Parent"));
        }

        // Create the Parent User
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
            // Create the ParentProfile
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

            // Create a test Dancer and a Ledger Entry so the dashboard has data
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

        // SEED ALL SPECIFIC INSTRUCTORS (WITH FULL NAMES) 
        var instructorsToSeed = new[]
        {
            ("Justine", "Reed"),
            ("Rylee", "Evers"),
            ("Sarah", "Howard"),
            ("Tessa", "Carter"),
            ("Natalie", "Lane"),
            ("Guest", "Instructor")
        };

        foreach (var (firstName, lastName) in instructorsToSeed)
        {
            string email = $"{firstName.ToLower()}@eatonvilledance.com";
            if (await userManager.FindByEmailAsync(email) == null)
            {
                ApplicationUser newInstructor = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(newInstructor, "Dance123!");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(newInstructor, "Instructor");
                }
            }
        }

        // FETCH REAL DATABASE IDs FOR ASSIGNMENT
        // We use the 'test instructor' as a fallback so the database never crashes
        string fallbackId = instructorUser?.Id ?? "";

        string justineId = (await userManager.FindByEmailAsync("justine@eatonvilledance.com"))?.Id ?? fallbackId;
        string ryleeId = (await userManager.FindByEmailAsync("rylee@eatonvilledance.com"))?.Id ?? fallbackId;
        string sarahId = (await userManager.FindByEmailAsync("sarah@eatonvilledance.com"))?.Id ?? fallbackId;
        string tessaId = (await userManager.FindByEmailAsync("tessa@eatonvilledance.com"))?.Id ?? fallbackId;
        string natalieId = (await userManager.FindByEmailAsync("natalie@eatonvilledance.com"))?.Id ?? fallbackId;
        string guestId = (await userManager.FindByEmailAsync("guest@eatonvilledance.com"))?.Id ?? fallbackId;

        // PROVISION CLASSES WITH REAL IDs 
        // TEMPORARY DEVELOPER OVERRIDE
        // This forcefully wipes out the old, image-less classes and their enrollments 
        // to guarantee the new seed data runs, bypassing SQL Server locks.
        /*var oldClasses = await context.DanceClasses.Include(c => c.Enrollments).ToListAsync();
        if (oldClasses.Any())
        {
            context.Enrollments.RemoveRange(oldClasses.SelectMany(c => c.Enrollments));
            context.DanceClasses.RemoveRange(oldClasses);
            await context.SaveChangesAsync();
        }*/
        if (!context.DanceClasses.Any())
        {
            List<DanceClass> starterClasses = new List<DanceClass>
            {
                new DanceClass
                {
                    ClassName = "Ballet 1",
                    Description = "A fun, introductory ballet class focusing on basic movement.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Beginner",
                    Capacity = 12, DayOfWeek = "Tuesday",
                    StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                    InstructorId = sarahId,
                    ImageUrl1 = "/images/classes/ballet-1.jpg",
                    ImageUrl2 = "/images/classes/ballet-7.jpg",
                    ImageUrl3 = "/images/classes/ballet-8.jpeg"

                },
                new DanceClass
                {
                    ClassName = "Ballet 2",
                    Description = "Recreation class. Ages 7 and up.",
                    MinimumAge = 7, MaximumAge = 99, RequiredSkillLevel = "Intermediate",
                    Capacity = 12, DayOfWeek = "Tuesday",
                    StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(18, 0, 0),
                    InstructorId = sarahId,
                    ImageUrl1 = "/images/classes/ballet-2.jpeg",
                    ImageUrl2 = "/images/classes/ballet-3.jpeg",
                    ImageUrl3 = "/images/classes/ballet-6.jpg"
                },
                new DanceClass
                {
                    ClassName = "Ballet 3",
                    Description = "Recreation class. Must be approved.",
                    MinimumAge = 7, MaximumAge = 99, RequiredSkillLevel = "Intermediate",
                    Capacity = 12, DayOfWeek = "Tuesday",
                    StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(19, 0, 0),
                    InstructorId = sarahId,
                    ImageUrl1 = "/images/classes/ballet-9.jpg",
                    ImageUrl2 = "/images/classes/ballet-2.jpeg",
                    ImageUrl3 = "/images/classes/ballet-5.jpg"
                },
                new DanceClass
                {
                    ClassName = "Pre pointe and Pointe",
                    Description = "Recreation class.",
                    MinimumAge = 10, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Tuesday",
                    StartTime = new TimeSpan(19, 0, 0), EndTime = new TimeSpan(19, 30, 0),
                    InstructorId = sarahId,
                    ImageUrl1 = "/images/classes/dancegroup-4.jpg",
                    ImageUrl2 = "/images/classes/dancepose-1.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-3.jpg"
                },
                new DanceClass
                {
                    ClassName = "Lyrical 1 and 2",
                    Description = "Recreation class. Ages 6 to 9.",
                    MinimumAge = 6, MaximumAge = 9, RequiredSkillLevel = "Beginner",
                    Capacity = 12, DayOfWeek = "Wednesday",
                    StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(18, 0, 0),
                    InstructorId = ryleeId,
                    ImageUrl1 = "/images/classes/dancepose-2.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-1.jpg",
                    ImageUrl3 = "/images/classes/dancepose-3.jpg"
                },
                new DanceClass
                {
                    ClassName = "Hip Hop 1",
                    Description = "Recreation class. Ages 5 to 8.",
                    MinimumAge = 5, MaximumAge = 8, RequiredSkillLevel = "Beginner",
                    Capacity = 12, DayOfWeek = "Thursday",
                    StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(16, 0, 0),
                    InstructorId = guestId,
                    ImageUrl1 = "/images/classes/hiphop-1.jpg",
                    ImageUrl2 = "/images/classes/hiphop-2.jpg",
                    ImageUrl3 = "/images/classes/hiphop-3.jpg"
                },
                new DanceClass
                {
                    ClassName = "Hip hop 2",
                    Description = "Recreation class. Ages 8 and up.",
                    MinimumAge = 8, MaximumAge = 99, RequiredSkillLevel = "Intermediate",
                    Capacity = 12, DayOfWeek = "Thursday",
                    StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                    InstructorId = natalieId,
                    ImageUrl1 = "/images/classes/hiphop-6.jpg",
                    ImageUrl2 = "/images/classes/hiphop-7.jpg",
                    ImageUrl3 = "/images/classes/hiphop-2.jpg"
                },
                new DanceClass
                {
                    ClassName = "Hip Hop 3",
                    Description = "Recreation class. Ages 11 and up.",
                    MinimumAge = 11, MaximumAge = 99, RequiredSkillLevel = "Intermediate",
                    Capacity = 12, DayOfWeek = "Thursday",
                    StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(18, 0, 0),
                    InstructorId = natalieId,
                    ImageUrl1 = "/images/classes/hiphop-3.jpg",
                    ImageUrl2 = "/images/classes/hiphop-4.jpg",
                    ImageUrl3 = "/images/classes/hiphop-5.jpg"
                },
                new DanceClass
                {
                    ClassName = "Intermediate Hip Hop",
                    Description = "High-energy hip hop routine focusing on choreography and stage presence.",
                    MinimumAge = 10, MaximumAge = 15, RequiredSkillLevel = "Intermediate",
                    Capacity = 20, DayOfWeek = "Wednesday",
                    StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(18, 0, 0),
                    InstructorId = fallbackId,
                    ImageUrl1 = "/images/classes/hiphop-4.jpg",
                    ImageUrl2 = "/images/classes/hiphop-6.jpg",
                    ImageUrl3 = "/images/classes/hiphop-8.jpg"
                },
                new DanceClass
                {
                    ClassName = "Tap and Jazz Combo",
                    Description = "A fun combo class for tap and jazz.",
                    MinimumAge = 5, MaximumAge = 8, RequiredSkillLevel = "Beginner",
                    Capacity = 12, DayOfWeek = "Monday",
                    StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(16, 0, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/acro-1.jpg",
                    ImageUrl2 = "/images/classes/dance.jpg",
                    ImageUrl3 = "/images/classes/dancepose-3.jpg"
                },
                new DanceClass
                {
                    ClassName = "Tech 1",
                    Description = "No recital. Works on fundamentals of dance.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Beginner",
                    Capacity = 12, DayOfWeek = "Monday",
                    StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/hiphop-1.jpg",
                    ImageUrl2 = "/images/classes/dance.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-1.jpg"
                },
                new DanceClass
                {
                    ClassName = "Tech 2",
                    Description = "No recital. Works on fundamentals of dance.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Intermediate",
                    Capacity = 12, DayOfWeek = "Monday",
                    StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                    InstructorId = fallbackId,
                    ImageUrl1 = "/images/classes/hiphop-1.jpg",
                    ImageUrl2 = "/images/classes/hiphop-5.jpg",
                    ImageUrl3 = "/images/classes/dancepose-2.jpg"
                },
                new DanceClass
                {
                    ClassName = "Tech 3",
                    Description = "Recreation class. No recital.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Intermediate",
                    Capacity = 12, DayOfWeek = "Wednesday",
                    StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(18, 0, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/dancegroup-4.jpg",
                    ImageUrl2 = "/images/classes/dancepose-2.jpg",
                    ImageUrl3 = "/images/classes/hiphop-2.jpg"
                },
                new DanceClass
                {
                    ClassName = "Creative movement 1",
                    Description = "Recreation class.",
                    MinimumAge = 3, MaximumAge = 4, RequiredSkillLevel = "Beginner",
                    Capacity = 10, DayOfWeek = "Wednesday",
                    StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(16, 45, 0),
                    InstructorId = fallbackId,
                    ImageUrl1 = "/images/classes/dancegroup-3.jpg",
                    ImageUrl2 = "/images/classes/dancepose-3.jpg",
                    ImageUrl3 = "/images/classes/hiphop-3.jpg"
                },
                new DanceClass
                {
                    ClassName = "Creative Movement 2",
                    Description = "Intro to movement for the little ones.",
                    MinimumAge = 3, MaximumAge = 4, RequiredSkillLevel = "Beginner",
                    Capacity = 10, DayOfWeek = "Tuesday",
                    StartTime = new TimeSpan(14, 45, 0), EndTime = new TimeSpan(15, 30, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/dancepose-3.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-4.jpg",
                    ImageUrl3 = "/images/classes/dancepose-1.jpg"
                },
                new DanceClass
                {
                    ClassName = "Lyrical and Jazz 1",
                    Description = "Recreation class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Beginner",
                    Capacity = 12, DayOfWeek = "Monday",
                    StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(18, 0, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/dancepose-1.jpg",
                    ImageUrl2 = "/images/classes/dancepose-3.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-1.jpg"
                },
                new DanceClass
                {
                    ClassName = "Lyrical and Jazz 2",
                    Description = "Recreation class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Intermediate",
                    Capacity = 12, DayOfWeek = "Monday",
                    StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(18, 0, 0),
                    InstructorId = ryleeId,
                    ImageUrl1 = "/images/classes/hiphop-1.jpg",
                    ImageUrl2 = "/images/classes/ballet-9.jpg",
                    ImageUrl3 = "/images/classes/dancepose-3.jpg"
                },
                new DanceClass
                {
                    ClassName = "Jazz 3",
                    Description = "Recreation class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Intermediate",
                    Capacity = 12, DayOfWeek = "Wednesday",
                    StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                    InstructorId = ryleeId,
                    ImageUrl1 = "/images/classes/dancepose-3.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-4.jpg",
                    ImageUrl3 = "/images/classes/dancepose-1.jpg"
                },
                new DanceClass
                {
                    ClassName = "Ballet Jazz combo",
                    Description = "Recreation class. Ages 5 to 8.",
                    MinimumAge = 5, MaximumAge = 8, RequiredSkillLevel = "Beginner",
                    Capacity = 12, DayOfWeek = "Wednesday",
                    StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(16, 0, 0),
                    InstructorId = guestId,
                    ImageUrl1 = "/images/classes/ballet-4.jpeg",
                    ImageUrl2 = "/images/classes/dancepose-3.jpg",
                    ImageUrl3 = "/images/classes/dancepose-2.jpg"
                },
                new DanceClass
                {
                    ClassName = "Acro 1",
                    Description = "Recreation class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Beginner",
                    Capacity = 12, DayOfWeek = "Tuesday",
                    StartTime = new TimeSpan(15, 30, 0), EndTime = new TimeSpan(16, 0, 0),
                    InstructorId = fallbackId,
                    ImageUrl1 = "/images/classes/acro-1.jpg",
                    ImageUrl2 = "/images/classes/acro-2.jpeg",
                    ImageUrl3 = "/images/classes/hiphop-1.jpg"
                },
                new DanceClass
                {
                    ClassName = "Acro 3",
                    Description = "Recreation class. Age 7 to 10.",
                    MinimumAge = 7, MaximumAge = 10, RequiredSkillLevel = "Intermediate",
                    Capacity = 12, DayOfWeek = "Thursday",
                    StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(16, 0, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/hiphop-1.jpg",
                    ImageUrl2 = "/images/classes/acro-1.jpg",
                    ImageUrl3 = "/images/classes/acro-2.jpeg"
                },
                new DanceClass
                {
                    ClassName = "Acro 4",
                    Description = "Recreation class. Highest level.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Monday",
                    StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(19, 0, 0),
                    InstructorId = ryleeId,
                    ImageUrl1 = "/images/classes/acro-1.jpg",
                    ImageUrl2 = "/images/classes/dancepose-3.jpg",
                    ImageUrl3 = "/images/classes/hiphop-6.jpg"
                },
                new DanceClass
                {
                    ClassName = "Acro 5",
                    Description = "Recreation class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Monday",
                    StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(19, 0, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/acro-2.jpeg",
                    ImageUrl2 = "/images/classes/dancepose-2.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-3.jpg"
                },
                new DanceClass
                {
                    ClassName = "Group 1",
                    Description = "Competition team class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Thursday",
                    StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(17, 30, 0),
                    InstructorId = fallbackId,
                    ImageUrl1 = "/images/classes/dancegroup-3.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-2.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-1.jpg"
                },
                new DanceClass
                {
                    ClassName = "Rylee Group 2",
                    Description = "Competition team class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Wednesday",
                    StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(18, 30, 0),
                    InstructorId = ryleeId,
                    ImageUrl1 = "/images/classes/dancegroup-4.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-3.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-2.jpg"
                },
                new DanceClass
                {
                    ClassName = "Justine group 3",
                    Description = "Competition team class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Tuesday",
                    StartTime = new TimeSpan(15, 30, 0), EndTime = new TimeSpan(16, 0, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/dancegroup-3.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-2.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-1.jpg"
                },
                new DanceClass
                {
                    ClassName = "Group 4",
                    Description = "Competition team class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Tuesday",
                    StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(16, 30, 0),
                    InstructorId = fallbackId,
                    ImageUrl1 = "/images/classes/dancegroup-2.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-3.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-4.jpg"
                },
                new DanceClass
                {
                    ClassName = "Group 5",
                    Description = "Competition team class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Tuesday",
                    StartTime = new TimeSpan(16, 30, 0), EndTime = new TimeSpan(17, 0, 0),
                    InstructorId = fallbackId,
                    ImageUrl1 = "/images/classes/dancegroup-3.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-2.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-1.jpg"
                },
                new DanceClass
                {
                    ClassName = "Justine Group 6",
                    Description = "Competition team class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Monday",
                    StartTime = new TimeSpan(19, 0, 0), EndTime = new TimeSpan(19, 30, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/dancegroup-4.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-3.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-2.jpg"
                },
                new DanceClass
                {
                    ClassName = "Group 7 Justine",
                    Description = "Competition team class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Wednesday",
                    StartTime = new TimeSpan(18, 30, 0), EndTime = new TimeSpan(19, 0, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/dancegroup-1.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-2.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-4.jpg"
                },
                new DanceClass
                {
                    ClassName = "Group 8 Justine",
                    Description = "Competition team class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Wednesday",
                    StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(18, 30, 0),
                    InstructorId = justineId,
                    ImageUrl1 = "/images/classes/dancegroup-3.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-1.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-2.jpg"
                },
                new DanceClass
                {
                    ClassName = "Comp 9",
                    Description = "Competition team class.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Advanced",
                    Capacity = 12, DayOfWeek = "Monday",
                    StartTime = new TimeSpan(19, 0, 0), EndTime = new TimeSpan(19, 30, 0),
                    InstructorId = ryleeId,
                    ImageUrl1 = "/images/classes/hiphop-5.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-1.jpg",
                    ImageUrl3 = "/images/classes/dancegroup-2.jpg"
                },
                new DanceClass
                {
                    ClassName = "Yoga",
                    Description = "Please contact 253-380-4061.",
                    MinimumAge = 16, MaximumAge = 99, RequiredSkillLevel = "Beginner",
                    Capacity = 15, DayOfWeek = "Tuesday",
                    StartTime = new TimeSpan(19, 0, 0), EndTime = new TimeSpan(20, 0, 0),
                    InstructorId = fallbackId
                },
                new DanceClass
                {
                    ClassName = "Stretch and strength",
                    Description = "Recreation class. No recital.",
                    MinimumAge = 5, MaximumAge = 99, RequiredSkillLevel = "Beginner",
                    Capacity = 12, DayOfWeek = "Thursday",
                    StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(19, 0, 0),
                    InstructorId = tessaId,
                    ImageUrl1 = "/images/classes/ballet-5.jpg",
                    ImageUrl2 = "/images/classes/dancegroup-2.jpg",
                    ImageUrl3 = "/images/classes/dancepose-1.jpg"
                },
                new DanceClass
                {
                    ClassName = "Ages 4 to 7",
                    Description = "Recreation class.",
                    MinimumAge = 4, MaximumAge = 7, RequiredSkillLevel = "Beginner",
                    Capacity = 12, DayOfWeek = "Thursday",
                    StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                    InstructorId = guestId, // Was Lindsey
                    ImageUrl1 = "/images/classes/hiphop-4.jpg",
                    ImageUrl2 = "/images/classes/ballet-7.jpg",
                    ImageUrl3 = "/images/classes/ballet-8.jpeg"
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
                    orphanedClass.InstructorId = fallbackId;
                }

                await context.SaveChangesAsync();
            }
        }

        // Seed the 2026-2027 competition and convention circuit
        await SeedCompetitionsAsync(context);
    }

    /// <summary>
    /// Seeds the 2026-2027 competition and convention schedule from the
    /// studio planning sheet, flagging active studio-attending events.
    /// </summary>
    private static async Task SeedCompetitionsAsync(
        ApplicationDbContext context)
    {
        if (await context.Competitions.AnyAsync())
        {
            return;
        }

        List<Competition> competitions = new List<Competition>
        {
            // Fall 2026 Conventions (2027 Season)
            new Competition
            {
                Name = "Platinum Convention",
                EventFormat = "Convention",
                VenueName = "The Westin Seattle",
                StartDate = new DateTime(2026, 9, 26),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Kar Convention",
                EventFormat = "Convention",
                VenueName = "Spokane Convention Center",
                StartDate = new DateTime(2026, 10, 18),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Celebrity Convention",
                EventFormat = "Convention",
                VenueName = "Hyatt Regency Lake Washington",
                StartDate = new DateTime(2026, 11, 20),
                EndDate = new DateTime(2026, 11, 22),
                IsStudioAttending = false
            },

            // January 2027
            new Competition
            {
                Name = "Rainbow",
                EventFormat = "Competition",
                VenueName = "Highline",
                StartDate = new DateTime(2027, 1, 15),
                EndDate = new DateTime(2027, 1, 17),
                IsStudioAttending = true
            },
            new Competition
            {
                Name = "Press Play Comp/Convention",
                EventFormat = "Comp/Convention",
                VenueName = "Bellevue",
                StartDate = new DateTime(2027, 1, 15),
                EndDate = new DateTime(2027, 1, 17),
                Notes = "Not leveled",
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Hollywood Connection",
                EventFormat = "Comp/Convention",
                VenueName = "Hyatt Regency Lake Washington",
                StartDate = new DateTime(2027, 1, 22),
                EndDate = new DateTime(2027, 1, 24),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Spotlight 1",
                EventFormat = "Competition",
                VenueName = "Highline",
                StartDate = new DateTime(2027, 1, 22),
                EndDate = new DateTime(2027, 1, 24),
                Notes = "Scholarships",
                IsStudioAttending = true
            },
            new Competition
            {
                Name = "Art of Movement Comp/Convention",
                EventFormat = "Comp/Convention",
                VenueName = "Hilton Seattle Airport",
                StartDate = new DateTime(2027, 1, 29),
                EndDate = new DateTime(2027, 1, 31),
                Notes = "Not leveled",
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "NUVO Comp/Convention",
                EventFormat = "Comp/Convention",
                VenueName = "Bellevue",
                StartDate = new DateTime(2027, 1, 29),
                EndDate = new DateTime(2027, 1, 31),
                Notes = "Not leveled",
                IsStudioAttending = false
            },

            // February 2027
            new Competition
            {
                Name = "Ultra",
                EventFormat = "Competition",
                VenueName = "Foster HS Tukwila",
                StartDate = new DateTime(2027, 2, 5),
                EndDate = new DateTime(2027, 2, 7),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Leap",
                EventFormat = "Competition",
                VenueName = "Highline",
                StartDate = new DateTime(2027, 2, 5),
                EndDate = new DateTime(2027, 2, 7),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Fluid Comp/Convention",
                EventFormat = "Comp/Convention",
                VenueName = "Hyatt Regency Lake Washington",
                StartDate = new DateTime(2027, 2, 5),
                EndDate = new DateTime(2027, 2, 7),
                Notes = "Not leveled",
                IsStudioAttending = true
            },
            new Competition
            {
                Name = "Ryze",
                EventFormat = "Competition",
                StartDate = new DateTime(2027, 2, 19),
                EndDate = new DateTime(2027, 2, 21),
                Notes = "Not leveled",
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Revive Comp/Convention",
                EventFormat = "Comp/Convention",
                StartDate = new DateTime(2027, 2, 19),
                EndDate = new DateTime(2027, 2, 21),
                Notes = "Not leveled",
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Revel Comp/Convention",
                EventFormat = "Comp/Convention",
                StartDate = new DateTime(2027, 2, 19),
                EndDate = new DateTime(2027, 2, 21),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Fly",
                EventFormat = "Competition",
                VenueName = "Renton Ikea Performing Arts",
                StartDate = new DateTime(2027, 2, 19),
                EndDate = new DateTime(2027, 2, 21),
                IsStudioAttending = true
            },
            new Competition
            {
                Name = "ID",
                EventFormat = "Competition",
                StartDate = new DateTime(2027, 2, 26),
                EndDate = new DateTime(2027, 2, 28),
                IsStudioAttending = false
            },

            // March 2027
            new Competition
            {
                Name = "Spotlight 2",
                EventFormat = "Competition",
                VenueName = "Tacoma Convention Center",
                StartDate = new DateTime(2027, 3, 5),
                EndDate = new DateTime(2027, 3, 7),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Jump Comp/Convention",
                EventFormat = "Comp/Convention",
                VenueName = "Portland, OR",
                StartDate = new DateTime(2027, 3, 5),
                EndDate = new DateTime(2027, 3, 7),
                Notes = "Not leveled",
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Encore",
                EventFormat = "Competition",
                VenueName = "Highline",
                StartDate = new DateTime(2027, 3, 5),
                EndDate = new DateTime(2027, 3, 7),
                Notes = "Leveled",
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Monster Dance",
                EventFormat = "Comp/Convention",
                StartDate = new DateTime(2027, 3, 5),
                EndDate = new DateTime(2027, 3, 7),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Energy",
                EventFormat = "Competition",
                VenueName = "Byrnes Performing Arts Arlington",
                StartDate = new DateTime(2027, 3, 12),
                EndDate = new DateTime(2027, 3, 14),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Adrenaline",
                EventFormat = "Comp/Convention",
                StartDate = new DateTime(2027, 3, 12),
                EndDate = new DateTime(2027, 3, 14),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Gravit8",
                EventFormat = "Competition",
                VenueName = "Tacoma Convention Center",
                StartDate = new DateTime(2027, 3, 12),
                EndDate = new DateTime(2027, 3, 14),
                Notes = "Scholarship Free Solo",
                IsStudioAttending = true
            },
            new Competition
            {
                Name = "Spotlight 3",
                EventFormat = "Competition",
                VenueName = "Renton Ikea Performing Arts",
                StartDate = new DateTime(2027, 3, 12),
                EndDate = new DateTime(2027, 3, 14),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Kar",
                EventFormat = "Competition",
                VenueName = "Highline",
                StartDate = new DateTime(2027, 3, 19),
                EndDate = new DateTime(2027, 3, 21),
                Notes = "Scholarships (Ultra)",
                IsStudioAttending = true
            },
            new Competition
            {
                Name = "Inferno",
                EventFormat = "Competition",
                StartDate = new DateTime(2027, 3, 19),
                EndDate = new DateTime(2027, 3, 21),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Thunderstruck",
                EventFormat = "Competition",
                VenueName = "Foster HS Tukwila",
                StartDate = new DateTime(2027, 3, 19),
                EndDate = new DateTime(2027, 3, 21),
                IsStudioAttending = false
            },

            // April 2027
            new Competition
            {
                Name = "Starquest",
                EventFormat = "Competition",
                VenueName = "Highline",
                StartDate = new DateTime(2027, 4, 2),
                EndDate = new DateTime(2027, 4, 4),
                Notes = "Scholarships",
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "24/7 Comp/Convention",
                EventFormat = "Comp/Convention",
                VenueName = "Bellevue",
                StartDate = new DateTime(2027, 4, 9),
                EndDate = new DateTime(2027, 4, 11),
                Notes = "Not leveled",
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Velocity Convention",
                EventFormat = "Convention",
                VenueName = "The Westin Seattle",
                StartDate = new DateTime(2027, 4, 9),
                EndDate = new DateTime(2027, 4, 11),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Act 1",
                EventFormat = "Competition",
                VenueName = "Portland, OR",
                StartDate = new DateTime(2027, 4, 9),
                EndDate = new DateTime(2027, 4, 11),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Starpower",
                EventFormat = "Competition",
                VenueName = "Highline",
                StartDate = new DateTime(2027, 4, 23),
                EndDate = new DateTime(2027, 4, 25),
                IsStudioAttending = true
            },
            new Competition
            {
                Name = "Vibe",
                EventFormat = "Competition",
                VenueName = "Vancouver",
                StartDate = new DateTime(2027, 4, 23),
                EndDate = new DateTime(2027, 4, 25),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "In10sity",
                EventFormat = "Competition",
                VenueName = "Tacoma Convention Center",
                StartDate = new DateTime(2027, 4, 30),
                EndDate = new DateTime(2027, 5, 2),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Radix Comp/Convention",
                EventFormat = "Comp/Convention",
                VenueName = "Oregon Convention Center",
                StartDate = new DateTime(2027, 4, 30),
                EndDate = new DateTime(2027, 5, 2),
                Notes = "Not leveled",
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Hollywood Vibe Comp/Convention",
                EventFormat = "Comp/Convention",
                VenueName = "The Westin Seattle",
                StartDate = new DateTime(2027, 4, 30),
                EndDate = new DateTime(2027, 5, 2),
                Notes = "Not leveled",
                IsStudioAttending = true
            },

            // May 2027
            new Competition
            {
                Name = "Groove",
                EventFormat = "Competition",
                VenueName = "Byrnes Performing Arts Arlington",
                StartDate = new DateTime(2027, 5, 7),
                EndDate = new DateTime(2027, 5, 9),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "NRG Dance Project",
                EventFormat = "Comp/Convention",
                StartDate = new DateTime(2027, 5, 14),
                EndDate = new DateTime(2027, 5, 16),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Showstopper",
                EventFormat = "Competition",
                VenueName = "Tacoma Convention Center",
                StartDate = new DateTime(2027, 5, 14),
                EndDate = new DateTime(2027, 5, 16),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Spotlight 4",
                EventFormat = "Competition",
                VenueName = "Byrnes Performing Arts Arlington",
                StartDate = new DateTime(2027, 5, 14),
                EndDate = new DateTime(2027, 5, 16),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Showstopper",
                EventFormat = "Competition",
                VenueName = "Tacoma Convention Center",
                StartDate = new DateTime(2027, 5, 21),
                EndDate = new DateTime(2027, 5, 23),
                IsStudioAttending = true
            },

            // June / July 2027 Nationals
            new Competition
            {
                Name = "Kar Nationals",
                EventFormat = "Competition",
                VenueName = "Panama City Beach, FL",
                StartDate = new DateTime(2027, 6, 15),
                EndDate = new DateTime(2027, 6, 20),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Spotlight Nationals",
                EventFormat = "Competition",
                VenueName = "Branson, MO",
                StartDate = new DateTime(2027, 6, 16),
                EndDate = new DateTime(2027, 6, 22),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Kar Nationals",
                EventFormat = "Competition",
                VenueName = "Pigeon Forge, TN",
                StartDate = new DateTime(2027, 6, 19),
                EndDate = new DateTime(2027, 6, 24),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Starquest Nationals",
                EventFormat = "Competition",
                VenueName = "Galveston, TX",
                StartDate = new DateTime(2027, 6, 20),
                EndDate = new DateTime(2027, 6, 25),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Fluid Nationals",
                EventFormat = "Comp/Convention",
                VenueName = "Scottsdale, AZ",
                StartDate = new DateTime(2027, 6, 23),
                EndDate = new DateTime(2027, 6, 27),
                Notes = "Not leveled",
                IsStudioAttending = true
            },
            new Competition
            {
                Name = "Energy Nationals",
                EventFormat = "Competition",
                VenueName = "Palm Springs, CA",
                StartDate = new DateTime(2027, 6, 25),
                EndDate = new DateTime(2027, 7, 2),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Fly Nationals",
                EventFormat = "Competition",
                VenueName = "Las Vegas, NV",
                StartDate = new DateTime(2027, 6, 25),
                EndDate = new DateTime(2027, 6, 28),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Fly Nationals",
                EventFormat = "Competition",
                VenueName = "Orlando, FL",
                StartDate = new DateTime(2027, 6, 25),
                EndDate = new DateTime(2027, 6, 28),
                IsStudioAttending = false
            },
            new Competition
            {
                Name = "Fly Nationals",
                EventFormat = "Competition",
                VenueName = "Wisconsin Dells, WI",
                StartDate = new DateTime(2027, 6, 29),
                EndDate = new DateTime(2027, 7, 2),
                IsStudioAttending = false
            }
        };

        context.Competitions.AddRange(competitions);
        await context.SaveChangesAsync();
    }
}
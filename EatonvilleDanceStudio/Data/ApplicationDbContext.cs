using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EatonvilleDanceStudio.Models;


namespace EatonvilleDanceStudio.Data;

/// <summary>
/// Represents the database context for the Eatonville Dance Studio application, extending the
/// IdentityDbContext to include identity management and database sets specific to the application's needs.
/// </summary>
/// <remarks>
/// The ApplicationDbContext serves as the bridge between the application and the database. It defines
/// the entity sets for the core models in the system, allowing for interaction with the database.
/// </remarks>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Maps the ParentProfile class to the ParentProfiles SQL table
    public DbSet<ParentProfile> ParentProfiles { get; set; }

    // Maps the Dancer class to the Dancers SQL table
    public DbSet<Dancer> Dancers { get; set; }
    
    public DbSet<CostumeOrder> CostumeOrders { get; set; }
    
    public DbSet<DanceClass> DanceClasses { get; set; }
    
    public DbSet<Enrollment> Enrollments { get; set; }
    
    public DbSet<LedgerEntry> LedgerEntries { get; set; }
    
    // Tracks instructor worked hours for payroll
    public DbSet<InstructorTimeLog> InstructorTimeLogs { get; set; }
    
    // Tracks instructor availability for private lessons
    public DbSet<PrivateLessonBlock> PrivateLessonBlocks { get; set; }
    
    // Tracks daily attendance for enrolled students
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    
    // Tracks instructor announcements for dance classes
    public DbSet<ClassAnnouncement> ClassAnnouncements { get; set; }
    
    // Maps the Invoice class to the Invoices SQL table
    public DbSet<Invoice> Invoices { get; set; }
    
    // Maps the StudioEvent class to the StudioEvents SQL table
    public DbSet<StudioEvent> StudioEvents { get; set; }
    
    // Maps the StudioMessage class to the StudioMessages SQL table
    public DbSet<StudioMessage> StudioMessages { get; set; }
}       
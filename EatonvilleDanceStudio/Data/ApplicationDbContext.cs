using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Dancer = EatonvilleDanceStudio.Models.Dancer;

namespace EatonvilleDanceStudio.Data;

/// <summary>
/// Represents the database context for the Eatonville Dance Studio
/// application, extending IdentityDbContext to include identity
/// management and database sets specific to the application's needs.
/// </summary>
/// <remarks>
/// The ApplicationDbContext serves as the bridge between the application
/// and the database. It defines the entity sets for the core models in
/// the system, allowing for interaction with the database.
/// </remarks>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    // Maps the ParentProfile class to the ParentProfiles SQL table
    public DbSet<ParentProfile> ParentProfiles { get; set; }

    // Maps the Dancer class to the Dancers SQL table
    public DbSet<Dancer> Dancers { get; set; }

    public DbSet<CostumeOrder> CostumeOrders { get; set; }

    public DbSet<LegalWaiver> LegalWaivers { get; set; }

    public DbSet<DanceClass> DanceClasses { get; set; }

    public DbSet<Enrollment> Enrollments { get; set; }

    public DbSet<LedgerEntry> LedgerEntries { get; set; }

    // Maps the FundraiserEvent class to the FundraiserEvents SQL table
    public DbSet<FundraiserEvent> FundraiserEvents { get; set; }

    // Tracks immutable fundraiser credit transactions per dancer
    public DbSet<FundraiserLedgerTransaction>
        FundraiserLedgerTransactions { get; set; }

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

    public DbSet<RehearsalVideo> RehearsalVideos { get; set; }

    public DbSet<Competition> Competitions { get; set; }

    public DbSet<CompetitionEntry> CompetitionEntries { get; set; }

    /// <summary>
    /// Intercepts synchronous database saves to prevent modifying or
    /// deleting existing fundraiser ledger transactions.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceImmutableFundraiserLedger();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Intercepts asynchronous database saves to prevent modifying or
    /// deleting existing fundraiser ledger transactions.
    /// </summary>
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceImmutableFundraiserLedger();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    /// <summary>
    /// Scans the EF Core ChangeTracker and throws an exception if any
    /// fundraiser ledger transaction is marked as Modified or Deleted.
    /// </summary>
    private void EnforceImmutableFundraiserLedger()
    {
        bool hasMutatedLedgerEntries = ChangeTracker
            .Entries<FundraiserLedgerTransaction>()
            .Any((EntityEntry<FundraiserLedgerTransaction> entry) =>
                entry.State == EntityState.Modified ||
                entry.State == EntityState.Deleted);

        if (hasMutatedLedgerEntries)
        {
            throw new InvalidOperationException(
                "Fundraiser ledger transactions are immutable and " +
                "cannot be modified or deleted once recorded.");
        }
    }
}
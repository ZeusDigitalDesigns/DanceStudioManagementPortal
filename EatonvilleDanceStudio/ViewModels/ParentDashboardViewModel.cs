using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.ViewModels;

public class ParentDashboardViewModel
{
    public required ParentProfile Profile { get; set; }
    public List<Dancer> Dancers { get; set; }
    
    // active enrollments
    public List<Enrollment> EnrolledClasses { get; set; } = new();
    
    // UI Placeholders
    public List<StudioMessage> Messages { get; set; } = new();
    public List<string> UpcomingEvents { get; set; } = new();
    public List<string> CompetitionPlacements { get; set; } = new();
    
    public decimal CurrentBalance { get; set; }
    
    // Gemini Supplied Code
    public List<LedgerEntry> RecentCharges { get; set; } = new();
    public List<LedgerEntry> RecentPayments { get; set; } = new();
}
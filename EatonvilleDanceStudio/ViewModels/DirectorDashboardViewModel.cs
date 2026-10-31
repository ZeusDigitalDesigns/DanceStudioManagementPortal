using System.Collections.Generic;
using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.ViewModels;

/// <summary>
/// Represents the data model for the Director's dashboard in the Eatonville Dance Studio application.
/// </summary>
/// <remarks>
/// This view model serves as a container for data displayed on the Director's dashboard,
/// including information about invoices that are either pending or late, as well as invoices that have been
/// recently paid. It helps organize and manage the invoice-related data for easy accessibility within the UI.
/// </remarks>
public class DirectorDashboardViewModel
{
    
    // Holds everything that is pending or late
    public List<Invoice> ActiveInvoices { get; set; } = new List<Invoice>();
    
    // Holds a quick reference of the most recently paid
    public List<Invoice> RecentPaidInvoices { get; set; } = new List<Invoice>();
    
    // Tracking studio wide revenue
    public decimal TotalOutstanding { get; set; }
    public decimal TotalPastDue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    
    // Holds the upcoming schedule for the calendar widget
    public List<StudioEvent> UpcomingEvents { get; set; } = new List<StudioEvent>();
}
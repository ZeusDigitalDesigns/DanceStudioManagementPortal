using System;
using System.Collections.Generic;

namespace DanceStudioManagementPortal.ViewModels
{
    /// <summary>
    /// Represents a single immutable ledger line item shown to parents,
    /// including the cumulative fundraising total after that event.
    /// </summary>
    public class ParentLedgerItemViewModel
    {
        public int TransactionId { get; set; }
        public int FundraiserEventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal RunningTotalAfterCredit { get; set; }
        public decimal BalanceAfterCredit { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    /// <summary>
    /// Summarizes an individual dancer's fundraising status for the parent portal,
    /// including outstanding balance, earned credits, and related credit history.
    /// </summary>
    public class DancerFundraiserSummaryViewModel
    {
        public int DancerId { get; set; }
        public string DancerName { get; set; } = string.Empty;
        public decimal CurrentOutstandingBalance { get; set; }
        public decimal TotalFundraisingCreditsEarned { get; set; }

        public List<ParentLedgerItemViewModel> CreditHistory { get; set; } 
            = new List<ParentLedgerItemViewModel>();
    }

    /// <summary>
    /// Provides the parent-facing fundraising overview for a family, including total earned credits
    /// and per-dancer fundraising summaries.
    /// </summary>
    public class ParentFundraiserPortalViewModel
    {
        public decimal FamilyRunningTotalCredits { get; set; }

        public List<DancerFundraiserSummaryViewModel> Dancers { get; set; } 
            = new List<DancerFundraiserSummaryViewModel>();
    }
}
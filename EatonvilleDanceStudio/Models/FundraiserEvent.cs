using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatonvilleDanceStudio.Models
{
    /// <summary>
    /// Defines how fundraiser net profit is divided among dancers.
    /// </summary>
    public enum AllocationMode
    {
        EqualSplit = 1,
        CustomAllocation = 2
    }

    /// <summary>
    /// Represents a studio fundraiser event logged by the Director.
    /// Tracks both public landing-page display details for visitors
    /// and internal accounting (revenue, expenses, net profit splits).
    /// </summary>
    public class FundraiserEvent
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter an event name.")]
        [StringLength(120)]
        [Display(Name = "Event Title")]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(14);

        // --- Public Home Page Bulletin Board Properties ---

        [Required(ErrorMessage = "Please enter a category badge.")]
        [StringLength(40)]
        [Display(Name = "Badge Label (e.g., Fundraiser, Bake Sale)")]
        public string CategoryBadge { get; set; } = "Fundraiser";

        [StringLength(120)]
        [Display(Name = "Time & Location Details")]
        public string TimeAndLocation { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Public Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Show on Public Home Page")]
        public bool IsActive { get; set; } = true;

        // --- Director Financial & Profit Allocation Properties ---

        [Range(0.00, 1000000.00)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Gross Revenue ($)")]
        public decimal GrossRevenue { get; set; }

        [Range(0.00, 1000000.00)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Expenses ($)")]
        public decimal Expenses { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Net Profit ($)")]
        public decimal NetProfit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Distributed ($)")]
        public decimal TotalDistributed { get; set; }

        [Display(Name = "Profit Split Mode")]
        public AllocationMode SplitMode { get; set; } =
            AllocationMode.EqualSplit;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public string CreatedByDirectorId { get; set; } = string.Empty;

        public ICollection<FundraiserLedgerTransaction> LedgerEntries
        {
            get;
            set;
        } = new List<FundraiserLedgerTransaction>();
    }
}
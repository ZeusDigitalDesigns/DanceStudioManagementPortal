using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EatonvilleDanceStudio.Models;

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
    /// Represents a studio fundraiser event logged by the Director,
    /// tracking gross revenue, expenses, and calculated net profit.
    /// </summary>
    public class FundraiserEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Range(0.01, 1000000.00)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossRevenue { get; set; }

        [Range(0.00, 1000000.00)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Expenses { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetProfit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDistributed { get; set; }

        public AllocationMode SplitMode { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public string CreatedByDirectorId { get; set; } = string.Empty;

        public ICollection<FundraiserLedgerTransaction> LedgerEntries { get; set; } = new List<FundraiserLedgerTransaction>();
    }
}
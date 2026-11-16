using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.ViewModels
{
    /// <summary>
    /// Captures selection state and custom credit input for one dancer.
    /// </summary>
    public class DancerAllocationInputViewModel
    {
        public int DancerId { get; set; }

        public string DancerName { get; set; } = string.Empty;

        public decimal CurrentBalance { get; set; }

        public bool IsSelected { get; set; }

        [Range(0.00, 1000000.00)]
        public decimal CustomCreditAmount { get; set; }
    }

    /// <summary>
    /// Form payload used by the Director to record a fundraiser event
    /// and distribute net profit across selected dancers.
    /// </summary>
    public class CreateFundraiserAllocationViewModel
    {
        [Required]
        [StringLength(120)]
        public string EventName { get; set; } 
            = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; } 
            = DateTime.Today;

        [Required]
        [Range(0.01, 1000000.00)]
        public decimal GrossRevenue { get; set; }

        [Required]
        [Range(0.00, 1000000.00)]
        public decimal Expenses { get; set; }

        [Required]
        public AllocationMode SplitMode { get; set; } 
            = AllocationMode.EqualSplit;

        public List<DancerAllocationInputViewModel> Allocations { get; set; } 
            = new List<DancerAllocationInputViewModel>();
    }
}
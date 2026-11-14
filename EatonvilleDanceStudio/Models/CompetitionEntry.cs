using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatonvilleDanceStudio.Models
{
    /// <summary>
    /// Represents a scheduled routine entry for a dance class
    /// within a competition.
    /// </summary>
    /// <remarks>
    /// Stores the routine name, performance order, backstage
    /// call time, optional stage time, and backstage notes for a
    /// competition schedule. Links a competition to the dance
    /// class performing the routine.
    /// </remarks>
    public class CompetitionEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompetitionId { get; set; }

        [ForeignKey("CompetitionId")]
        public Competition? Competition { get; set; }

        [Required(ErrorMessage = "Please select a dance class.")]
        [Display(Name = "Dance Class")]
        public int DanceClassId { get; set; }

        [ForeignKey("DanceClassId")]
        public DanceClass? DanceClass { get; set; }

        [Required(ErrorMessage = "Please enter the routine name.")]
        [StringLength(120)]
        [Display(Name = "Routine Name")]
        public string RoutineName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Entry / Performance Order")]
        public int PerformanceOrder { get; set; }

        [Required]
        [Display(Name = "Backstage Call Time")]
        public DateTime BackstageCallTime { get; set; }

        [Display(Name = "Stage Time")]
        public DateTime? StageTime { get; set; }

        [StringLength(300)]
        [Display(Name = "Hair, Costume & Backstage Notes")]
        public string? BackstageNotes { get; set; }
    }
}
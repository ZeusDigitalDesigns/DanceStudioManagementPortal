using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EatonvilleDanceStudio.Models
{
    /// <summary>
    /// Represents a dance competition or event attended or tracked by Eatonville Dance Studio.
    /// </summary>
    /// <remarks>
    /// Stores event-level details such as the competition name, format, venue information, date range,
    /// studio attendance status, and notes. Also serves as the parent entity for scheduled competition
    /// entries associated with dance class routines.
    /// </remarks>
    public class Competition
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter the competition name.")]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Event Format")]
        public string EventFormat { get; set; } = "Competition";

        [StringLength(150)]
        [Display(Name = "Venue / Location")]
        public string? VenueName { get; set; }

        [StringLength(250)]
        [Display(Name = "Street Address (For Maps)")]
        public string? VenueAddress { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Studio Attending")]
        public bool IsStudioAttending { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }

        public ICollection<CompetitionEntry> Entries { get; set; }
            = new List<CompetitionEntry>();
    }
}
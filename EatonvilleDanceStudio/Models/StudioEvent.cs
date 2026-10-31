using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EatonvilleDanceStudio.Data;

namespace EatonvilleDanceStudio.Models
{
    /// <summary>
    /// Represents an event scheduled at the Eatonville Dance Studio.
    /// The event can include classes, competitions, rehearsals, or other types of activities.
    /// </summary>
    /// <remarks>
    /// The StudioEvent class contains details about a specific studio event,
    /// such as its title, description, type, schedule, room allocation, capacity,
    /// and the assigned instructor. It also includes navigation properties for
    /// relationships to related entities like the instructor.
    /// </remarks>
    public class StudioEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        // Class, Competition, Rehearsal, etc.
        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = "Class";

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        // Tracks which physical room the event is in (e.g., Main Studio, upstairs, etc.)
        [Required]
        [StringLength(50)]
        public string Room { get; set; } = "Main Studio";

        // Hard cap on how many students can register
        [Required]
        public int Capacity { get; set; }

        // The ID of the Instructor assigned to run the event
        public string? InstructorId { get; set; }

        // Navigation property to the identity user (Instructor)
        [ForeignKey("InstructorId")]
        public ApplicationUser? Instructor { get; set; }
    }
}
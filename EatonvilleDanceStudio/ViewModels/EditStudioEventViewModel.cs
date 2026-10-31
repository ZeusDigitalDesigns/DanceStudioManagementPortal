using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EatonvilleDanceStudio.ViewModels
{
    /// <summary>
    /// Represents a view model used to edit studio event details in the application.
    /// This class encapsulates properties needed for event management including
    /// identification, event metadata, and dropdown options for user interface integration.
    /// </summary>
    public class EditStudioEventViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        [Required]
        public string Room { get; set; } = string.Empty;

        [Required]
        [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100.")]
        public int Capacity { get; set; }

        [Display(Name = "Assigned Instructor")]
        public string? InstructorId { get; set; }

        // Dropdown Lists for the UI 
        
        public List<SelectListItem> InstructorsList { get; set; } 
            = new List<SelectListItem>();
        
        public List<SelectListItem> EventTypesList { get; set; } 
            = new List<SelectListItem>();
            
        public List<SelectListItem> RoomsList { get; set; } 
            = new List<SelectListItem>();
    }
}
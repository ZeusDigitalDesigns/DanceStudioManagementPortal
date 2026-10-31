using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EatonvilleDanceStudio.ViewModels
{
    /// <summary>
    /// Represents the view model used for creating a new studio event.
    /// </summary>
    /// <remarks>
    /// This class contains details required to define a studio event, including
    /// event specifics such as title, description, type, scheduling, room, capacity,
    /// and instructor assignment. It is primarily used to bind form data and
    /// support dropdown list options for UI components.
    /// </remarks>
    public class CreateStudioEventViewModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = "Class";

        [Required]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; } = DateTime.Today.AddHours(12);

        [Required]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; } = DateTime.Today.AddHours(13);

        [Required]
        public string Room { get; set; } = "Main Studio";

        // Hard cap on how many students can register
        [Required]
        [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100.")]
        public int Capacity { get; set; } = 20;

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
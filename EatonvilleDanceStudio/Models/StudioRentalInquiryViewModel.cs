using System;
using System.ComponentModel.DataAnnotations;

namespace EatonvilleDanceStudio.Models
{
    /// <summary>
    /// Holds public studio room rental details and room-availability
    /// inquiry fields submitted by visitors or prospective renters.
    /// </summary>
    public class StudioRentalInquiryViewModel
    {
        public string RoomSpecsSummary { get; set; } =
            "Sprung dance floors, full-wall mirrors, and studio sound";

        public decimal HourlyRate { get; set; } = 45.00m;

        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100)]
        [Display(Name = "Your Name")]
        public string RenterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email.")]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        public string RenterEmail { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Preferred Rental Date")]
        public DateTime PreferredDate { get; set; } =
            DateTime.Today.AddDays(7);

        [Required(ErrorMessage = "Please enter hours or time window.")]
        [StringLength(100)]
        [Display(Name = "Requested Time / Hours")]
        public string RequestedHours { get; set; } = string.Empty;

        [StringLength(400)]
        [Display(Name = "Event or Rehearsal Details")]
        public string? EventDetails { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EatonvilleDanceStudio.ViewModels
{
    /// <summary>
    /// Handles form input for uploading a local video file or
    /// linking an external practice video to a specific class.
    /// </summary>
    public class RehearsalVideoUploadViewModel
    {
        [Required(ErrorMessage = "Please enter a routine title.")]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a class.")]
        [Display(Name = "Class")]
        public int DanceClassId { get; set; }

        [Display(Name = "Upload Formatted Video (.mp4)")]
        public IFormFile? VideoFile { get; set; }

        [Display(Name = "Or External / Pre-loaded Video Path")]
        [StringLength(300)]
        public string? ExternalOrLocalUrl { get; set; }
    }
}
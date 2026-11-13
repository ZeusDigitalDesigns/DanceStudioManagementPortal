using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatonvilleDanceStudio.Models;

/// <summary>
/// Represents a rehearsal video associated with a dance class in the Eatonville Dance Studio system.
/// </summary>
/// <remarks>
/// Stores identifying information, descriptive metadata, the video location, upload timestamp,
/// and the relationship to the dance class for which the rehearsal video is intended.
/// The video location may refer to either a locally stored file or an external video URL.
/// </remarks>
public class RehearsalVideo
{
    [Key]
    public int RehearsalVideoId { get; set; }
    
    [Required(ErrorMessage = "Please enter a routine title.")]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    // Stores either a local path or and external Url.
    [Required(ErrorMessage = "A video file or link is required.")]
    [StringLength(300)]
    public string VideoUrl { get; set; } = string.Empty;
    
    // true if stored locally in wwwroot/videos/rehearsals
    public bool IsLocalFile { get; set; }
    
    public DateTime? UpLoadedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    [Display(Name = "Dance Class")]
    public int DanceClassId { get; set; }
    
    [ForeignKey("DanceClassId")]
    public DanceClass? DanceClass { get; set; }
    
}
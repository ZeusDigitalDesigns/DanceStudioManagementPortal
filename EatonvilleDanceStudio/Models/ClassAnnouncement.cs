using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EatonvilleDanceStudio.Models;

/// <summary>
/// Represents an announcement made for a specific dance class.
/// Includes details such as the announcement title, message, associated class,
/// and the date the announcement was posted.
/// </summary>

public class ClassAnnouncement
{
    [Key]
    public int ClassId { get; set; }
    
    // Links this announcement to a specific dance class
    [Required]
    public int DanceClassId { get; set; }
    
    [ForeignKey("DanceClassId")]
    [BindNever] // Explicitly blocks the model binder from mapping this from a web request.
    public DanceClass? DanceClass { get; set; }
    
    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Announcement Message")]
    public string Message { get; set; } = string.Empty;
    
    // This will automatically set the date when the announcement is created
    public DateTime DatePosted { get; set; } = DateTime.Now;
}
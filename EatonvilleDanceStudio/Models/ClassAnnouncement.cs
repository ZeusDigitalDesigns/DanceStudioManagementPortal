using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatonvilleDanceStudio.Models;

public class ClassAnnouncement
{
    [Key]
    public int ClassId { get; set; }
    
    // Links this announcement to a specific dance class
    [Required]
    public int DanceClassId { get; set; }
    
    [ForeignKey("DanceClassId")]
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
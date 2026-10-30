using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatonvilleDanceStudio.Models;

/// <summary>
/// Represents a dancer enrolled at Eatonville Dance Studio.
/// </summary>
/// <remarks>
/// This class includes properties for identifying a dancer, associating them with a parent profile,
/// detailing their skill level, and managing their photo and enrollment information.
/// </remarks>

public class Dancer
{
    [Key]
    public int Id { get; set; }
    
    // Foreign key linking the dancer to the parent
    [Required]
    public int ParentProfileId { get; set; }
    
    [ForeignKey("ParentProfileId")]
    public ParentProfile Parent { get; set; }
    
    [Required]
    [StringLength(50)]
    [Display(Name = "Dancer First Name")]
    public required string FirstName { get; set; }
    
    [Required]
    [StringLength(50)]
    [Display(Name = "Dancer Last Name")]
    public required string LastName { get; set; }
    
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Dancer Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    // --- New Properties for Issue #16 ---
    
    // Beginner, Intermediate, Advanced
    [Required]
    [StringLength(30)]
    [Display(Name = "Skill Level")]
    public string SkillLevel { get; set; } = "Beginner"; 

    public string? ProfilePhotoPath { get; set; }
    
    // None, Pending, Approved, Rejected
    [StringLength(20)]
    public string PhotoApprovalStatus { get; set; } = "None";

    [Required]
    [StringLength(10)]
    [Display(Name = "Account Status")]
    public string AccountStatus { get; set; } = "Approved";
    
    
    // Navigation property for enrollments, representing the dancer's enrollments in one or more dance classes.
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public string ParentId { get; set; }
}
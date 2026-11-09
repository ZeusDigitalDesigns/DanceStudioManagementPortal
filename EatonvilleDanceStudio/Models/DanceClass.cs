using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EatonvilleDanceStudio.Data;

namespace EatonvilleDanceStudio.Models;

/// <summary>
/// Represents a dance class offered by Eatonville Dance Studio.
/// Includes details about the class, such as its name, age range, skill level requirement,
/// scheduling information, and enrollment capacity.
/// </summary>

public class DanceClass
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(100), Display(Name = "Class Name")]
    public required string ClassName { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }

    [Required, Display(Name = "Minimum Age")]
    public int MinimumAge { get; set; }

    [Required, Display(Name = "Maximum Age")]
    public int MaximumAge { get; set; }

    [Required, StringLength(30), Display(Name = "Required Skill Level")]
    public string RequiredSkillLevel { get; set; } = "Beginner";

    [Required, Display(Name = "Class Capacity")]
    public int Capacity { get; set; }
    
    // Scheduling Details 

    [StringLength(15), Display(Name = "Day of Week")]
    public string? DayOfWeek { get; set; }

    [Required, DataType(DataType.Time), Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; }

    [Required, DataType(DataType.Time), Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; }
    
    // Navigation Properties
    
    // Links the class to a specific instructor
    public string? InstructorId { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.ForeignKey("InstructorId")]
    public ApplicationUser? Instructor { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Monthly Cost")]
    public decimal MonthlyCost { get; set; }
    
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    
    [StringLength(255)]
    public string? ImageUrl1 { get; set; }
    
    [StringLength(255)]
    public string? ImageUrl2 { get; set; }
    
    [StringLength(255)]
    public string? ImageUrl3 { get; set; }
}
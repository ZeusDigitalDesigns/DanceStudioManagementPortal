using System.ComponentModel.DataAnnotations;

namespace EatonvilleDanceStudio.ViewModels;

public class AddDancerViewModel
{
    
    [Required]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public required string FirstName { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public required string LastName { get; set; }
    
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; }
    
    [StringLength(30)]
    [Display(Name = "Skill Level")]
    [RegularExpression("^(Beginner|Intermediate|Advanced)$", ErrorMessage = "Invalid skill level selected.")]
    public string SkillLevel { get; set; } = "Beginner";
    
    [StringLength(200)]
    [Display(Name = "Medical / Allergy Alerts (Leave blank if none)")]
    public string? MedicalAlerts { get; set; }
}
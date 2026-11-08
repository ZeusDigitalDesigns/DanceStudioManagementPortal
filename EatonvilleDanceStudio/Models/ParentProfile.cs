using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EatonvilleDanceStudio.Data;

namespace EatonvilleDanceStudio.Models;

public class ParentProfile
{
    [Key]
    public int Id { get; set; }
    
    //Links the profile to the IdentityUser auth table
    [Required]
    public required string UserId { get; set; }

    [ForeignKey("UserId")]
    public required ApplicationUser? User { get; set; }
    
    [Required]
    [Display(Name = "First Name")]
    [StringLength(50)]
    public required string FirstName { get; set; }
    
    [Required]
    [Display(Name = "Last Name")]
    [StringLength(50)]
    public required string LastName { get; set; }
    
    [Required]
    [Phone]
    [Display(Name = "Primary Number")]
    [StringLength(20)]
    public required string PrimaryPhone { get; set; }
    
    [Required]
    [Display(Name = "Emergency Contact Name")]
    [StringLength(50)]
    public required string EmergencyContactName { get; set; }
    
    [Required]
    [Phone]
    [Display(Name = "Emergency Contact Number")]
    [StringLength(20)]
    public required string EmergencyContactPhone { get; set; }
    
    [Display(Name = "Military or First Responder")]
    public bool IsMilitaryOrFirstResponder { get; set; } = false;
    
    [Required]
    [Display(Name = "Account Status")]
    [StringLength(10)]
    public required string AccountStatus { get; set; } = "Pending";
    
    // Navigation property: a parent can have many dancers.
    public ICollection<Dancer> Dancers { get; set; } = new List<Dancer>();
    
    // Legal & Compliance
    public ICollection<LegalWaiver> LegalWaivers { get; set; }
}
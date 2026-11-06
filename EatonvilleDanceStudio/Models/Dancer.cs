using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatonvilleDanceStudio.Models;

/// <summary>
/// Represents a dancer participating in the Eatonville Dance Studio.
/// </summary>
public class Dancer
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ParentProfileId { get; set; }
    
    // Links to ASP.NET Identity User (Nullable because toddlers won't have an account)
    public string? UserId { get; set; }
    
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
    
    // Calculates age automatically from DateOfBirth
    // This doesn't get saved to SQL, it just calculates on the fly.
    public int Age 
    { 
        get 
        {
            if (!DateOfBirth.HasValue) return 0;
            DateTime today = DateTime.Today;
            int age = today.Year - DateOfBirth.Value.Year;
            
            // Subtract a year if they haven't had their birthday yet this year
            if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
            
            return age;
        }
    }
    
    [StringLength(200)]
    [Display(Name = "Medical / Allergy Alerts")]
    public string? MedicalAlerts { get; set; }
    
    [Required]
    [StringLength(30)]
    [Display(Name = "Skill Level")]
    public string SkillLevel { get; set; } = "Beginner"; 

    public string? ProfilePhotoPath { get; set; }
    
    [StringLength(20)]
    public string PhotoApprovalStatus { get; set; } = "None";

    [Required]
    [StringLength(10)]
    [Display(Name = "Account Status")]
    public string AccountStatus { get; set; } = "Approved";
    
    public ICollection<Enrollment> Enrollments { get; set; } 
        = new List<Enrollment>();
    
    // Standard Sizing
    [Display(Name = "Leotard Size")]
    public string? LeotardSize { get; set; }
    
    [Display(Name = "T-Shirt / Hoodie Size")]
    public string? TopSize { get; set; }
        
    [Display(Name = "Shoe Size")]
    public string? ShoeSize { get; set; }
    
    // Exact Measurements
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Girth { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "Bust/Chest")]
    public decimal? BustChest { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Waist { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Hips { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Inseam { get; set; }

    // Navigation property for their costume orders
    public ICollection<CostumeOrder>? CostumeOrders { get; set; }
}
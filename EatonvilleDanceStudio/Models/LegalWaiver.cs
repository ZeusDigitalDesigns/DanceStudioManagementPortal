using System;
using System.ComponentModel.DataAnnotations;

namespace EatonvilleDanceStudio.Models;

public class LegalWaiver
{
    
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ParentProfileId { get; set; }
    public ParentProfile? ParentProfile { get; set; }

    [Required]
    [StringLength(100)]
    public string WaiverType { get; set; } = "Liability, Medical & Photo Release";
    
    [Required]
    [StringLength(20)]
    public string Season { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "You must type your full legal name to agree.")]
    [StringLength(150)]
    [Display(Name = "Digital Signature")]
    public string TypedSignature { get; set; } = string.Empty;
    
    [Required]
    public DateTime SignedDate { get; set; } = DateTime.Now;
    
    [StringLength(50)]
    public string? IPAddress { get; set; }
}
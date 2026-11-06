using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatonvilleDanceStudio.Models;

public class CostumeOrder
{
    [Key]
    public int Id { get; set; }

    // Which dancer is this for?
    [Required]
    public int DancerId { get; set; }
    
    public Dancer? Dancer { get; set; }

    // Which class is this costume associated with?
    [Required]
    public int DanceClassId { get; set; }
    
    public DanceClass? DanceClass { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Costume Name/Description")]
    public string CostumeName { get; set; } = string.Empty;

    // Issue #32 requirement: Provide links to popular Costume providers
    [Url]
    [Display(Name = "Vendor Link")]
    public string? VendorLink { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    // --- Lifecycle Tracking ---
    
    [Display(Name = "Payment Status")]
    public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Paid

    // Statuses: Not Ordered, Ordered, Shipped, Received at Studio, Handed Out
    [Display(Name = "Order Status")]
    public string OrderStatus { get; set; } = "Not Ordered"; 
    

    [Display(Name = "Expected Delivery")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [Display(Name = "Date Handed to Dancer")]
    public DateTime? HandedOutDate { get; set; }
}
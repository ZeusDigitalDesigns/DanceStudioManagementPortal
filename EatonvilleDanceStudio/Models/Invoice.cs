using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EatonvilleDanceStudio.Data;

namespace EatonvilleDanceStudio.Models
{
    // Represents a manual or automated invoice billed to a parent/user
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        // The ID of the user responsible for this invoice
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Navigation property to the identity user
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // What the charge is for (e.g., "Late Fee", "Private Lesson")
        [Required]
        [StringLength(150)]
        public string Description { get; set; } = string.Empty;

        // The total amount owed
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime DueDate { get; set; }

        // Tracks if it has been paid, pending, or overdue
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";
    }
}
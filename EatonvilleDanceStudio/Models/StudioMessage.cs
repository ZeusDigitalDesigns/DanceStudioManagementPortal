using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EatonvilleDanceStudio.Data;

namespace EatonvilleDanceStudio.Models;

/// <summary>
/// Represents a message entity within the Eatonville Dance Studio application,
/// facilitating communication between users and supporting both individual and broadcast messages.
/// </summary>
/// <remarks>
/// The <c>StudioMessage</c> class encapsulates the essential data required for messages,
/// such as sender, receiver, subject, body content, and metadata about the message's state.
/// </remarks>
public class StudioMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;

    [ForeignKey("SenderId")]
    public ApplicationUser? Sender { get; set; }

    // Nullable: If null, this is a blast to all active accounts
    public string? ReceiverId { get; set; }

    [ForeignKey("ReceiverId")]
    public ApplicationUser? Receiver { get; set; }

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.Now;

    public bool IsRead { get; set; } = false;
}

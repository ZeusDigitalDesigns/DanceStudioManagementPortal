using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EatonvilleDanceStudio.ViewModels;

/// <summary>
/// Represents the view model for composing a message within the system.
/// </summary>
public class ComposeMessageViewModel
{
    [Display(Name = "Recipient (Leave unselected for Studio-Wide Blast)")]
    public string? ReceiverId { get; set; }

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    [DataType(DataType.MultilineText)]
    public string Body { get; set; } = string.Empty;

    // Dropdown list containing every active user in the system
    public List<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();
}

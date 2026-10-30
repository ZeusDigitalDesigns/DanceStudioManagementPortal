using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EatonvilleDanceStudio.ViewModels;

/// <summary>
/// A ViewModel for creating invoices in the invoice management system.
/// </summary>
/// <remarks>
/// This ViewModel contains the necessary data for creating an invoice,
/// including the user being billed, a description of the invoice, the amount,
/// and the due date. It also provides a dropdown list of users for selection.
/// This data is validated to ensure correctness before the invoice is created.
/// </remarks>
public class CreateInvoiceViewModel
{
    [Required(ErrorMessage = "Please select a user to bill.")]
    [Display(Name = "Bill To")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

    // This will hold the dropdown list of parents/users
    public List<SelectListItem> UsersList { get; set; } 
        = new List<SelectListItem>();
}

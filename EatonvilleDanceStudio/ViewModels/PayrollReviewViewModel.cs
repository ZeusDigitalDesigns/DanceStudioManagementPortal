using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.ViewModels;

/// <summary>
/// Represents the view model for reviewing payroll data, including time logs,
/// date range for the review, and total hours worked by instructors within the specified date range.
/// </summary>
/// <remarks>
/// This view model is intended to be used for payroll review screens, where administrators
/// can review instructor time logs and calculate the total hours worked for payroll processing.
/// </remarks>
public class PayrollReviewViewModel
{
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }
    
    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }
    
    public List<InstructorTimeLog> TimeLogs { get; set; } = new List<InstructorTimeLog>();

    
    // Calculates the total hours worked by instructors within the specified date range
    public decimal TotalHours => TimeLogs.Sum(t => t.Hours);
}
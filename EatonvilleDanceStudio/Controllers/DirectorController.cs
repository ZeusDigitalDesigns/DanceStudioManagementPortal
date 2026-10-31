using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Controllers;

[Authorize(Roles = "Director")]
public class DirectorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    
    // Inject UserManager so we can pull the list of parents for the dropdown
    public DirectorController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    // GET: /Director/PendingAccounts
    
    /// <summary>
    /// Retrieves a list of all parent profiles with a pending account status.
    /// </summary>
    /// <return>
    /// An IActionResult containing a view with a collection of parent profiles
    /// where the account status is "Pending".
    /// </return>
    public async Task<IActionResult> PendingAccounts()
    {
        // Fetch all profiles where the status is still pending
        List<ParentProfile> pending = await _context.ParentProfiles
            .Where(p => p.AccountStatus == "Pending")
            .ToListAsync();

        return View(pending);
    }
    
    // POST: /Director/Approve

    /// <summary>
    /// Approves a parent profile by updating its account status to "Approved".
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the parent profile to be approved.
    /// </param>
    /// <returns>
    /// An IActionResult that redirects to the PendingAccounts view.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        // Explicitly type the profile instead of using var
        ParentProfile? profile = await _context.ParentProfiles.FindAsync(id);
        
        if (profile != null)
        {
            profile.AccountStatus = "Approved";
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(PendingAccounts));
    }

    // GET: /Director/CreateInvoice

    /// <summary>
    /// Displays a form for creating a new invoice, allowing the director to select a user to bill
    /// and specify invoice details such as description, amount, and due date.
    /// </summary>
    /// <returns>
    /// An IActionResult containing the view for creating a new invoice, pre-populated with a list of users
    /// to select as the recipient of the invoice.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> CreateInvoice()
    {
        CreateInvoiceViewModel viewModel = new CreateInvoiceViewModel();
        
        // Fetch all users to populate the "Bill To" dropdown
        List<ApplicationUser> users = 
            await _userManager.Users.ToListAsync();
        
        viewModel.UsersList = users.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = $"{u.FirstName} {u.LastName} ({u.Email})"
        }).ToList();

        return View(viewModel);
    }

    // POST: /Director/CreateInvoice

    /// <summary>
    /// Creates a new invoice for a specified user based on the provided details.
    /// </summary>
    /// <param name="model">
    /// An instance of CreateInvoiceViewModel containing the user ID, invoice description,
    /// amount, and other related invoice details.
    /// </param>
    /// <returns>
    /// An IActionResult that redirects to the ManageUsers view upon successful creation of the invoice,
    /// or reloads the current view with validation messages if the model state is invalid.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvoice(
        CreateInvoiceViewModel model)
    {
        if (ModelState.IsValid)
        {
            Invoice newInvoice = new Invoice
            {
                UserId = model.UserId,
                Description = model.Description,
                Amount = model.Amount,
                DueDate = model.DueDate,
                CreatedDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.Invoices.Add(newInvoice);
            await _context.SaveChangesAsync();

            // Send the boss back to the dashboard
            return RedirectToAction("ManageUsers"); 
        }
        


        // If form fails validation, reload the dropdown to prevent crashes
        List<ApplicationUser> users = 
            await _userManager.Users.ToListAsync();
        
        model.UsersList = users.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = $"{u.FirstName} {u.LastName} ({u.Email})"
        }).ToList();

        return View(model);
    }

    /// <summary>
    /// Manages user interactions by displaying a dashboard for the Director.
    /// The dashboard includes active invoices, recent paid invoices, total outstanding balance,
    /// total past due amounts, and revenue generated for the current month.
    /// </summary>
    /// <returns>
    /// An IActionResult containing a view populated with the DirectorDashboardViewModel,
    /// which provides details about invoices and related financial data.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> ManageUsers()
    {
        DirectorDashboardViewModel viewModel = new DirectorDashboardViewModel();

        // Fetch all Pending or Overdue invoices.
        // .Include(i => i.User) = access to the parent's name
        viewModel.ActiveInvoices = await _context.Invoices
            .Include(i => i.User)
            .Where(i => i.Status != "Paid")
            .OrderBy(i => i.DueDate)
            .ToListAsync();

        // Fetch the last 10 Paid invoices for quick reference.
        viewModel.RecentPaidInvoices = await _context.Invoices
            .Include(i => i.User)
            .Where(i => i.Status == "Paid")
            .OrderByDescending(i => i.CreatedDate)
            .Take(10)
            .ToListAsync();
        
        // Calculate total outstanding (everything unpaid)
        viewModel.TotalOutstanding = await _context.Invoices
            .Where(i => i.Status != "Paid")
            .SumAsync(i => i.Amount);
        
        // Calculate Total PastDue (unpaid and late)
        viewModel.TotalPastDue = await _context.Invoices
            .Where(i => i.Status != "Paid" && i.DueDate < DateTime.Now)
            .SumAsync(i => i.Amount);
        
        // Calculate Revenue This Month (Paid within the current month)
        DateTime startOfMonth = new DateTime(
            DateTime.Now.Year, DateTime.Now.Month, 1);
        
        viewModel.RevenueThisMonth = await _context.Invoices
            .Where(i => i.Status == "Paid" && i.CreatedDate >= startOfMonth)
            .SumAsync(i => i.Amount);

        return View(viewModel);
    }
    
    // POST: /Director/MarkInvoicePaid

    /// <summary>
    /// Marks the specified invoice as "Paid" and updates the database accordingly.
    /// Redirects to the ManageUsers view after processing.
    /// </summary>
    /// <param name="invoiceId">
    /// The unique identifier of the invoice to be marked as paid.
    /// </param>
    /// <returns>
    /// An IActionResult that redirects the user to the ManageUsers view.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkInvoicePaid(int invoiceId)
    {
        Invoice? invoice = await _context.Invoices.FindAsync(invoiceId);
    
        if (invoice != null)
        {
            invoice.Status = "Paid";
            await _context.SaveChangesAsync();
        }

        // Refresh the dashboard so the invoice moves to the "Recently Paid" table
        return RedirectToAction(nameof(ManageUsers));
    }
}

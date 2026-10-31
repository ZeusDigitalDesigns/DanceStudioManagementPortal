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

        // Calculate Upcoming Events (upcoming events within the next 5 days)
        viewModel.UpcomingEvents = await _context.StudioEvents
            .Include(e => e.Instructor)
            .Where(e => e.StartTime > DateTime.Now)
            .OrderBy(e => e.StartTime)
            .Take(5)
            .ToListAsync();
        
        
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

    /// <summary>
    /// Prepares and displays a form for creating a new studio event.
    /// The form is pre-populated with dropdown options for instructors, event types, and room selections.
    /// </summary>
    /// <return>
    /// An IActionResult containing a view populated with a CreateStudioEventViewModel.
    /// </return>
    [HttpGet]
    public async Task<IActionResult> CreateEvent()
    {
        CreateStudioEventViewModel viewModel = new CreateStudioEventViewModel();
    
        // Fetch only users who are officially Instructors
        IList<ApplicationUser> instructors = 
            await _userManager.GetUsersInRoleAsync("Instructor");
        
        viewModel.InstructorsList = instructors.Select(i => new SelectListItem
        {
            Value = i.Id,
            Text = $"{i.FirstName} {i.LastName}"
        }).ToList();

        // Populate static dropdowns for the studio's operational choices
        viewModel.EventTypesList = new List<SelectListItem>
        {
            new SelectListItem { Value = "Class", Text = "Standard Class" },
            new SelectListItem { Value = "Private", Text = "Private Lesson" },
            new SelectListItem { Value = "Comp Group 1", Text = "Comp Group 1" },
            new SelectListItem { Value = "Comp Group 2", Text = "Comp Group 2" },
            new SelectListItem { Value = "Comp Group 3", Text = "Comp Group 3" },
            new SelectListItem { Value = "Comp Group 4", Text = "Comp Group 4" },
            new SelectListItem { Value = "Comp Group 5", Text = "Comp Group 5" },
            new SelectListItem { Value = "Rehearsal", Text = "Rehearsal" }
        };

        viewModel.RoomsList = new List<SelectListItem>
        {
            new SelectListItem { Value = "Main Studio", Text = "Main Studio" },
            new SelectListItem { Value = "Upstairs", Text = "Upstairs" }
        };

        return View(viewModel);
    }
    
    // POST: /Director/CreateEvent
    /// <summary>
    /// Handles the creation of a new studio event by processing submitted form data
    /// and saving the event to the database if the provided data is valid.
    /// </summary>
    /// <param name="model">
    /// The view model containing details of the event to be created, including its title,
    /// description, event type, start and end time, room, capacity, and instructor.
    /// </param>
    /// <return>
    /// An IActionResult that redirects to the "ManageUsers" view upon successful event creation.
    /// If the model validation fails, it returns the same view with reloaded dropdown values.
    /// </return>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(
        CreateStudioEventViewModel model)
    {
        if (ModelState.IsValid)
        {
            StudioEvent newEvent = new StudioEvent
            {
                Title = model.Title,
                Description = model.Description,
                EventType = model.EventType,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Room = model.Room,
                Capacity = model.Capacity,
                InstructorId = model.InstructorId
            };

            _context.StudioEvents.Add(newEvent);
            await _context.SaveChangesAsync();

            // Send the boss back to the dashboard once the event is saved
            return RedirectToAction("ManageUsers");
        }

        // If validation fails, we MUST reload the dropdowns 
        // to prevent the view from crashing
        IList<ApplicationUser> instructors = 
            await _userManager.GetUsersInRoleAsync("Instructor");
        
        model.InstructorsList = instructors.Select(i => new SelectListItem
        {
            Value = i.Id,
            Text = $"{i.FirstName} {i.LastName}"
        }).ToList();

        model.EventTypesList = new List<SelectListItem>
        {
            new SelectListItem { Value = "Class", Text = "Standard Class" },
            new SelectListItem { Value = "Private", Text = "Private Lesson" },
            new SelectListItem { Value = "Comp Group 1", Text = "Comp Group 1" },
            new SelectListItem { Value = "Comp Group 2", Text = "Comp Group 2" },
            new SelectListItem { Value = "Comp Group 3", Text = "Comp Group 3" },
            new SelectListItem { Value = "Comp Group 4", Text = "Comp Group 4" },
            new SelectListItem { Value = "Comp Group 5", Text = "Comp Group 5" },
            new SelectListItem { Value = "Rehearsal", Text = "Rehearsal" }
        };

        model.RoomsList = new List<SelectListItem>
        {
            new SelectListItem { Value = "Main Studio", Text = "Main Studio" },
            new SelectListItem { Value = "Studio B", Text = "Studio B" },
            new SelectListItem { Value = "Studio C", Text = "Studio C" }
        };

        return View(model);
    }
    
    // POST: /Director/DeleteEvent

    /// <summary>
    /// Deletes a specified studio event based on its unique identifier.
    /// </summary>
    /// <param name="eventId">
    /// The unique identifier of the studio event to be deleted.
    /// </param>
    /// <return>
    /// An IActionResult that redirects to the ManageUsers view after the event
    /// is deleted or if the event is not found.
    /// </return>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int eventId)
    {
        // Find the event in the database
        StudioEvent? eventToDelete = await _context.StudioEvents.FindAsync(eventId);
    
        if (eventToDelete != null)
        {
            // Remove it and save changes
            _context.StudioEvents.Remove(eventToDelete);
            await _context.SaveChangesAsync();
        }

        // Refresh the dashboard
        return RedirectToAction(nameof(ManageUsers));
    }
    
    // GET: /Director/EditEvent/{id}
    /// <summary>
    /// Retrieves the details of a specific studio event for editing.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the studio event to be edited.
    /// </param>
    /// <return>
    /// An IActionResult that renders a view populated with the studio event's details
    /// and associated dropdown lists for editing the event.
    /// </return>
    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        StudioEvent? studioEvent = await _context.StudioEvents.FindAsync(id);
    
        if (studioEvent == null)
        {
            return NotFound(); // Failsafe if the event was already deleted
        }

        EditStudioEventViewModel viewModel = new EditStudioEventViewModel
        {
            Id = studioEvent.Id,
            Title = studioEvent.Title,
            Description = studioEvent.Description,
            EventType = studioEvent.EventType,
            StartTime = studioEvent.StartTime,
            EndTime = studioEvent.EndTime,
            Room = studioEvent.Room,
            Capacity = studioEvent.Capacity,
            InstructorId = studioEvent.InstructorId
        };

        // Load the dropdown lists
        IList<ApplicationUser> instructors = 
            await _userManager.GetUsersInRoleAsync("Instructor");
        
        viewModel.InstructorsList = instructors.Select(i => new SelectListItem
        {
            Value = i.Id,
            Text = $"{i.FirstName} {i.LastName}"
        }).ToList();

        viewModel.EventTypesList = new List<SelectListItem>
        {
            new SelectListItem { Value = "Class", Text = "Standard Class" },
            new SelectListItem { Value = "Private", Text = "Private Lesson" },
            new SelectListItem { Value = "Comp Group 1", Text = "Comp Group 1" },
            new SelectListItem { Value = "Comp Group 2", Text = "Comp Group 2" },
            new SelectListItem { Value = "Comp Group 3", Text = "Comp Group 3" },
            new SelectListItem { Value = "Comp Group 4", Text = "Comp Group 4" },
            new SelectListItem { Value = "Comp Group 5", Text = "Comp Group 5" },
            new SelectListItem { Value = "Rehearsal", Text = "Rehearsal" }
        };

        viewModel.RoomsList = new List<SelectListItem>
        {
            new SelectListItem { Value = "Main Studio", Text = "Main Studio" },
            new SelectListItem { Value = "Upstairs", Text = "Upstairs" }
        };

        return View(viewModel);
    }
    
    // POST: /Director/EditEvent/{id}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditEvent(int id, EditStudioEventViewModel model)
{
    // Security check to ensure the URL ID matches the form ID
    if (id != model.Id)
    {
        return NotFound();
    }

    if (ModelState.IsValid)
    {
        StudioEvent? eventToUpdate = await _context.StudioEvents.FindAsync(id);
        
        if (eventToUpdate == null)
        {
            return NotFound();
        }

        // Apply the updated values
        eventToUpdate.Title = model.Title;
        eventToUpdate.Description = model.Description;
        eventToUpdate.EventType = model.EventType;
        eventToUpdate.StartTime = model.StartTime;
        eventToUpdate.EndTime = model.EndTime;
        eventToUpdate.Room = model.Room;
        eventToUpdate.Capacity = model.Capacity;
        eventToUpdate.InstructorId = model.InstructorId;

        await _context.SaveChangesAsync();
        
        // Kick back to the dashboard to see the updated schedule
        return RedirectToAction(nameof(ManageUsers));
    }

    // If validation fails, reload those dropdowns so the view doesn't crash
    IList<ApplicationUser> instructors = 
        await _userManager.GetUsersInRoleAsync("Instructor");
        
    model.InstructorsList = instructors.Select(i => new SelectListItem
    {
        Value = i.Id,
        Text = $"{i.FirstName} {i.LastName}"
    }).ToList();

    model.EventTypesList = new List<SelectListItem>
    {
        new SelectListItem { Value = "Class", Text = "Standard Class" },
        new SelectListItem { Value = "Private", Text = "Private Lesson" },
        new SelectListItem { Value = "Comp Group 1", Text = "Comp Group 1" },
        new SelectListItem { Value = "Comp Group 2", Text = "Comp Group 2" },
        new SelectListItem { Value = "Comp Group 3", Text = "Comp Group 3" },
        new SelectListItem { Value = "Comp Group 4", Text = "Comp Group 4" },
        new SelectListItem { Value = "Comp Group 5", Text = "Comp Group 5" },
        new SelectListItem { Value = "Rehearsal", Text = "Rehearsal" }
    };

    model.RoomsList = new List<SelectListItem>
    {
        new SelectListItem { Value = "Main Studio", Text = "Main Studio" },
        new SelectListItem { Value = "Upstairs", Text = "Upstairs" }
    };

    return View(model);
}
}

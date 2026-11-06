using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.ViewModels;
using EatonvilleDanceStudio.Services;
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
    private readonly ITuitionCalculatorService _tuitionService;

    // Inject UserManager so we can pull the list of parents for the dropdown
    public DirectorController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ITuitionCalculatorService tuitionService)
    {
        _context = context;
        _userManager = userManager;
        _tuitionService = tuitionService;
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
    /// An IActionResult that redirects to the ManageUsers view upon successful creation of the invoice
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

    //GET
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

    //GET
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
    /// <summary>
    /// Updates an existing studio event using the submitted event details.
    /// </summary>
    /// <param name="id">
    /// The identifier of the studio event to update.
    /// </param>
    /// <param name="model">
    /// The view model containing the updated studio event information.
    /// </param>
    /// <return>
    /// An IActionResult that redirects to the user management view when the update succeeds,
    /// returns NotFound when the event identifier is invalid or the event cannot be found,
    /// or returns the edit view with validation errors and required selection lists when the model is invalid.
    /// </return>
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

    //GET
    /// <summary>
    /// Retrieves a view displaying a summary of payroll activity for instructors within a specified date range.
    /// The default date range is the last 14 days if no dates are provided.
    /// </summary>
    /// <param name="startDate">The beginning of the date range for filtering payroll data. If null, defaults to 14 days prior today.</param>
    /// <param name="endDate">The end of the date range for filtering payroll data. If null, defaults to today.</param>
    /// <return>
    /// An IActionResult containing a view of the payroll review, presenting instructor time logs filtered by the specified date range.
    /// </return>
    [HttpGet]
    public async Task<IActionResult> PayrollReview(DateTime? startDate, DateTime? endDate)
    {
        // Default to the last 14 days if no custom dates are provided
        DateTime start = startDate ?? DateTime.Today.AddDays(-14);
        DateTime end = endDate ?? DateTime.Today;

        PayrollReviewViewModel viewModel = new PayrollReviewViewModel
        {
            StartDate = start,
            EndDate = end
        };
        
        // Fetch logs within the date range, include the Instructor Data so we have their names
        // Assumes your DbSet in ApplicationDBContext is named InstructorTimeLogs
        viewModel.TimeLogs = await _context.InstructorTimeLogs
            .Include(t => t.Instructor)
            .Where(t => t.Date >= start && t.Date <= end)
            .OrderByDescending(t => t.Date)
            .ThenBy(t => t.Instructor!.LastName)
            .ToListAsync();
        
        return View(viewModel);
    }
    
    //GET
    /// <summary>
    /// Exports payroll data for instructors to a CSV file within the specified date range.
    /// </summary>
    /// <param name="startDate">
    /// The start date of the payroll period. If null, defaults to 14 days before today's date.
    /// </param>
    /// <param name="endDate">
    /// The end date of the payroll period. If null, defaults to today's date.
    /// </param>
    /// <returns>
    /// A CSV file containing payroll information for instructors, including date, instructor name, hours worked, notes, status, and a summary of total hours.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> ExportPayrollCsv(DateTime? startDate, DateTime? endDate)
    {
        DateTime start = startDate ?? DateTime.Today.AddDays(-14);
        DateTime end = endDate ?? DateTime.Today;

        var logs = await _context.InstructorTimeLogs
            .Include(t => t.Instructor)
            .Where(t => t.Date >= start && t.Date <= end)
            .OrderByDescending(t => t.Date)
            .ThenBy(t => t.Instructor!.LastName)
            .ToListAsync();

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Date,Instructor,Hours,Notes,Status");

        foreach (var log in logs)
        {
            string instructorName = log.Instructor != null 
                ? $"{log.Instructor.FirstName} {log.Instructor.LastName}" 
                : "Unknown";
            
            // Escape quotes and commas in the description just in case
            string safeNotes = $"\"{log.Description?.Replace("\"", "\"\"")}\"";
            string status = log.IsApproved ? "Approved" : "Pending";

            builder.AppendLine($"{log.Date.ToShortDateString()},{instructorName},{log.Hours:0.00},{safeNotes},{status}");
        }

        // Add a total row at the bottom
        decimal totalHours = logs.Sum(l => l.Hours);
        builder.AppendLine($",TOTAL HOURS,{totalHours:0.00},,");

        return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"Payroll_{start:yyyyMMdd}_to_{end:yyyyMMdd}.csv");
    }
    
    // GET: /Director/Messages
    /// <summary>
    /// Retrieves a collection of messages for the currently authenticated user,
    /// including both received messages (inbox) and sent messages.
    /// </summary>
    /// <returns>
    /// An IActionResult containing a view with a MessagesViewModel.
    /// The model contains two collections:
    /// 1. 'Inbox' - Messages received by the user or sent studio-wide.
    /// 2. 'Sent' - Messages sent by the user.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> Messages()
    {
        string? currentUserId = _userManager.GetUserId(User);
    
        if (currentUserId == null) 
        {
            return NotFound("User not authenticated.");
        }

        MessagesViewModel viewModel = new MessagesViewModel();

        // INBOX: Messages directed to you specifically, or studio-wide blasts (ReceiverId is null)
        viewModel.Inbox = await _context.StudioMessages
            .Include(m => m.Sender)
            .Where(m => m.ReceiverId == currentUserId || m.ReceiverId == null)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        // SENT: Messages where you are the sender
        viewModel.Sent = await _context.StudioMessages
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == currentUserId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        return View(viewModel);
    }
    
    // GET: /Director/ComposeMessage
    /// <summary>
    /// Displays a form for composing a message, allowing the selection of a recipient
    /// from a list of users or targeting a studio-wide broadcast.
    /// </summary>
    /// <return>
    /// An IActionResult containing a view with a pre-populated ComposeMessageViewModel,
    /// including the list of users sorted alphabetically by last name.
    /// </return>
    [HttpGet]
    public IActionResult ComposeMessage()
    {
        ComposeMessageViewModel viewModel = new ComposeMessageViewModel();
    
        // Fetch all users and sort them alphabetically by last name for the dropdown
        var allUsers = _userManager.Users.OrderBy(u => u.LastName).ToList();
    
        viewModel.UsersList = allUsers.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = $"{u.FirstName} {u.LastName} ({u.Email})"
        }).ToList();

        return View(viewModel);
    }
    
    //POST /Director/ComposeMessage
    /// <summary>
    /// Sends a message from the currently logged-in Director to a specified user
    /// or broadcasts it to all users if no recipient is specified.
    /// </summary>
    /// <param name="model">
    /// An instance of ComposeMessageViewModel containing the message details,
    /// such as recipient, subject, and body, as well as a list of potential recipients.
    /// </param>
    /// <returns>
    /// An IActionResult that redirects to the ManageUsers view upon successful message sending,
    /// or re-displays the ComposeMessage view with validation errors and updated user list if validation fails.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ComposeMessage(ComposeMessageViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Get the currently logged-in Director's ID
            string? senderId = _userManager.GetUserId(User);
        
            if (senderId != null)
            {
                StudioMessage newMessage = new StudioMessage
                {
                    SenderId = senderId,
                    
                    // If ReceiverId is null, it acts as a studio blast
                    ReceiverId = model.ReceiverId, 
                    Subject = model.Subject,
                    Body = model.Body,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.StudioMessages.Add(newMessage);
                await _context.SaveChangesAsync();

                // Kick back to the dashboard once the message flies
                return RedirectToAction(nameof(ManageUsers));
            }
        }

        // If validation fails, reload the dropdown so the view doesn't crash
        var allUsers = _userManager.Users.OrderBy(u => u.LastName).ToList();
    
        model.UsersList = allUsers.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = $"{u.FirstName} {u.LastName} ({u.Email})"
        }).ToList();

        return View(model);
    }
    
    // POST: /Director/RunMonthlyBilling
    /// <summary>
    /// Generates monthly tuition invoices for all parent profiles with billable dancer enrollments.
    /// </summary>
    /// <return>
    /// An IActionResult that redirects to the user management view after creating pending invoices
    /// and storing a success message with the number of invoices generated.
    /// </return>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunMonthlyBilling()
    {
        // Fetch all parents with their dancers and enrollments
        List<ParentProfile> parents = await _context.ParentProfiles
            .Include(p => p.Dancers)
                .ThenInclude(d => d.Enrollments)
                    .ThenInclude(e => e.DanceClass)
            .ToListAsync();

        int invoicesGenerated = 0;

        // Loop through each parent and run the engine
        foreach (ParentProfile parent in parents)
        {
            List<Dancer> familyDancers = parent.Dancers.ToList();
            
            decimal monthlyTuition = _tuitionService.CalculateMonthlyTuition(
                parent, 
                familyDancers);

            if (monthlyTuition > 0)
            {
                // Create the Ledger Entry / Invoice for this month
                Invoice newInvoice = new Invoice
                {
                    UserId = parent.UserId, // Links to Identity ApplicationUser
                    Amount = monthlyTuition, 
                    Description = $"Tuition - {DateTime.Now:MMMM yyyy}",
                    CreatedDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(15), // Gives them 15 days to pay
                    Status = "Pending"
                };

                _context.Invoices.Add(newInvoice);
                invoicesGenerated++;
            }
        }

        // 4. Save all new invoices to the database in one shot
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = 
            $"Generated {invoicesGenerated} invoices for {DateTime.Now:MMMM}.";
            
        return RedirectToAction(nameof(ManageUsers));
    }
    
    // GET: /Director/CostumeManagement
    /// <summary>
    /// Retrieves costume orders for director review, including related dancer and dance class information.
    /// </summary>
    /// <return>
    /// An IActionResult containing a view with the costume orders ordered by expected delivery date.
    /// </return>
    [HttpGet]
    public async Task<IActionResult> CostumeManagement()
    {
        List<CostumeOrder> orders = await _context.CostumeOrders
            .Include(c => c.Dancer)
            .Include(c => c.DanceClass)
            .OrderBy(c => c.ExpectedDeliveryDate)
            .ToListAsync();
            
        return View(orders);
    }
    
    // GET: /Director/EditDancer/{id}
    /// <summary>
    /// Retrieves the dancer with the specified identifier for editing.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the dancer to edit.
    /// </param>
    /// <return>
    /// An IActionResult containing the edit view for the matching dancer, or a NotFound result if no dancer exists with the specified identifier.
    /// </return>
    [HttpGet]
    public async Task<IActionResult> EditDancer(int id)
    {
        Dancer? dancer = await _context.Dancers.FindAsync(id);

        if (dancer == null)
        {
            return NotFound();
        }

        return View(dancer);
    }

    // POST: /Director/EditDancer/{id}
    /// <summary>
    /// Updates an existing dancer record and saves the submitted dancer information.
    /// </summary>
    /// <param name="id">
    /// The identifier of the dancer record to edit.
    /// </param>
    /// <param name="model">
    /// The dancer data submitted for the update.
    /// </param>
    /// <return>
    /// An IActionResult that redirects to the costume management dashboard when the update succeeds, returns the edit view when validation fails, or returns NotFound when the route identifier does not match the submitted dancer record.
    /// </return>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDancer(int id, Dancer model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            _context.Update(model);
            await _context.SaveChangesAsync();

            // Send them back to the Costume Management dashboard after saving sizes
            return RedirectToAction(nameof(CostumeManagement));
        }

        return View(model);
    }
    
    // GET: /Director/DancerRoster
    /// <summary>
    /// Retrieves the dancer roster, including parent information, ordered by dancer last name and first name.
    /// </summary>
    /// <return>
    /// An IActionResult containing a view with the full list of dancers and their associated parent profiles.
    /// </return>
    [HttpGet]
    public async Task<IActionResult> DancerRoster()
    {
        List<Dancer> dancers = await _context.Dancers
            .Include(d => d.Parent)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync();

        return View(dancers);
    }
    
    // GET: /Director/CreateCostumeOrder
    /// <summary>
    /// Displays the form for creating a new costume order, including available dancers and dance classes for selection.
    /// </summary>
    /// <return>
    /// An IActionResult containing the costume order creation view initialized with a new CostumeOrder model.
    /// </return>
    [HttpGet]
    public async Task<IActionResult> CreateCostumeOrder()
    {
        // Prep dropdown data for Dancers (FullName) and Classes
        var dancers = await _context.Dancers
            .Select(d => new { d.Id, FullName = d.FirstName + " " + d.LastName })
            .OrderBy(d => d.FullName)
            .ToListAsync();
            
        ViewBag.Dancers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            dancers, "Id", "FullName");
            
        ViewBag.Classes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _context.DanceClasses.OrderBy(c => c.ClassName).ToListAsync(), 
            "Id", "ClassName");

        return View(new CostumeOrder());
    }

    // POST: /Director/CreateCostumeOrder
    /// <summary>
    /// Creates a costume order from the submitted model and saves it when validation succeeds.
    /// </summary>
    /// <param name="model">
    /// The costume order data containing the selected dancer and dance class.
    /// </param>
    /// <return>
    /// An IActionResult that redirects to costume management when the order is saved,
    /// or returns the view with validation details and restored selection lists when validation fails.
    /// </return>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCostumeOrder(CostumeOrder model)
    {
        // Ignore navigation properties so ModelState doesn't fail validation
        ModelState.Remove(nameof(model.Dancer));
        ModelState.Remove(nameof(model.DanceClass));

        if (ModelState.IsValid)
        {
            _context.CostumeOrders.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CostumeManagement));
        }

        // Repopulate dropdowns if validation fails
        var dancers = await _context.Dancers
            .Select(d => new { d.Id, FullName = d.FirstName + " " + d.LastName })
            .OrderBy(d => d.FullName)
            .ToListAsync();
            
        ViewBag.Dancers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            dancers, "Id", "FullName", model.DancerId);
            
        ViewBag.Classes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _context.DanceClasses.OrderBy(c => c.ClassName).ToListAsync(), 
            "Id", "ClassName", model.DanceClassId);

        return View(model);
    }
} 


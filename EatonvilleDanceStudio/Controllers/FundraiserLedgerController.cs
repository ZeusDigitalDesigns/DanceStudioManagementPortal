using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DanceStudioManagementPortal.ViewModels;
using EatonvilleDanceStudio.ViewModels;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using EatonvilleDanceStudio.Services;
using EatonvilleDanceStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.Controllers;

/// <summary>
/// Controller managing Director fundraiser credit distributions
/// and Parent fundraising running-total views.
/// </summary>
[Authorize]
public class FundraiserLedgerController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly FundraiserAccountingService _accountingService;

    public FundraiserLedgerController(
        ApplicationDbContext dbContext,
        FundraiserAccountingService accountingService)
    {
        _dbContext = dbContext;
        _accountingService = accountingService;
    }

    /// <summary>
    /// Displays the fundraiser recording and credit allocation tool
    /// exclusively for the Studio Director.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Create()
    {
        List<DancerAllocationInputViewModel> dancerRows =
            await _dbContext.Dancers
                .AsNoTracking()
                .OrderBy((Dancer d) => d.FullName)
                .Select((Dancer d) => new DancerAllocationInputViewModel
                {
                    DancerId = d.Id,
                    DancerName = d.FullName,
                    CurrentBalance = d.OutstandingBalance,
                    IsSelected = false,
                    CustomCreditAmount = 0m
                })
                .ToListAsync();

        CreateFundraiserAllocationViewModel viewModel =
            new CreateFundraiserAllocationViewModel
            {
                EventDate = DateTime.Today,
                SplitMode = AllocationMode.EqualSplit,
                Allocations = dancerRows
            };

        return View(viewModel);
    }

    /// <summary>
    /// Processes the Director's fundraiser submission and atomically
    /// applies credits to the selected dancers' balances.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Create(
        CreateFundraiserAllocationViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        string directorId =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Director";

        try
        {
            FundraiserEvent createdEvent =
                await _accountingService.RecordAndDistributeAsync(
                    viewModel,
                    directorId);

            TempData["SuccessMessage"] =
                $"Fundraiser '{createdEvent.EventName}' logged and " +
                $"${createdEvent.TotalDistributed:F2} credited.";

            return RedirectToAction(nameof(Create));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(viewModel);
        }
    }

    /// <summary>
    /// Allows Parent profiles to view their running total of
    /// fundraising money and each applied ledger credit.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Parent,Director")]
    public async Task<IActionResult> MyCredits()
    {
        string currentUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        ParentFundraiserPortalViewModel summary =
            await _accountingService.GetParentFundraiserSummaryAsync(
                currentUserId);

        return View(summary);
    }
}
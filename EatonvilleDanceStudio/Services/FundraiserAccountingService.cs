using DanceStudioManagementPortal.ViewModels;
using EatonvilleDanceStudio.ViewModels;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EatonvilleDanceStudio.Services;

/// <summary>
/// Handles fundraiser profit calculation, allocation validation,
/// atomic balance updates, and parent running-total queries.
/// </summary>
public class FundraiserAccountingService
{
    private readonly ApplicationDbContext _dbContext;

    public FundraiserAccountingService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Validates and atomically records a fundraiser event and applies
    /// ledger credits to all selected dancers.
    /// </summary>
    public async Task<FundraiserEvent> RecordAndDistributeAsync(
        CreateFundraiserAllocationViewModel input,
        string directorUserId)
    {
        decimal netProfit = input.GrossRevenue - input.Expenses;
        if (netProfit <= 0m)
        {
            throw new InvalidOperationException(
                "Net profit must be greater than zero to distribute.");
        }

        List<DancerAllocationInputViewModel> selectedDancers =
            input.Allocations
                .Where((DancerAllocationInputViewModel a) => a.IsSelected)
                .ToList();

        if (selectedDancers.Count == 0)
        {
            throw new InvalidOperationException(
                "Select at least one dancer to receive credits.");
        }

        Dictionary<int, decimal> plannedCredits =
            CalculateAllocations(
                selectedDancers,
                netProfit,
                input.SplitMode);

        decimal totalDistributed = plannedCredits.Values.Sum();
        if (totalDistributed > netProfit)
        {
            throw new InvalidOperationException(
                "Total distributed credits cannot exceed net profit.");
        }

        using IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            DateTime nowUtc = DateTime.UtcNow;

            FundraiserEvent fundraiser = new FundraiserEvent
            {
                EventName = input.EventName.Trim(),
                EventDate = input.EventDate,
                GrossRevenue = input.GrossRevenue,
                Expenses = input.Expenses,
                NetProfit = netProfit,
                TotalDistributed = totalDistributed,
                SplitMode = input.SplitMode,
                CreatedAtUtc = nowUtc,
                CreatedByDirectorId = directorUserId
            };

            _dbContext.FundraiserEvents.Add(fundraiser);
            await _dbContext.SaveChangesAsync();

            List<int> dancerIds = plannedCredits.Keys.ToList();
            List<Dancer> dancersToCredit = await _dbContext.Dancers
                .Where((Dancer d) => dancerIds.Contains(d.Id))
                .ToListAsync();

            if (dancersToCredit.Count != dancerIds.Count)
            {
                throw new InvalidOperationException(
                    "One or more selected dancer profiles were not found.");
            }

            foreach (Dancer dancer in dancersToCredit)
            {
                decimal creditAmount = plannedCredits[dancer.Id];
                decimal previousBalance = dancer.OutstandingBalance;
                decimal updatedBalance = previousBalance - creditAmount;

                dancer.OutstandingBalance = updatedBalance;

                FundraiserLedgerTransaction ledgerEntry =
                    new FundraiserLedgerTransaction
                    {
                        FundraiserEventId = fundraiser.Id,
                        DancerId = dancer.Id,
                        CreditAmount = creditAmount,
                        PreviousBalance = previousBalance,
                        NewBalance = updatedBalance,
                        TimestampUtc = nowUtc,
                        RecordedByDirectorId = directorUserId
                    };

                _dbContext.FundraiserLedgerTransactions.Add(ledgerEntry);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return fundraiser;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Computes per-dancer credit amounts based on the chosen mode.
    /// </summary>
    private Dictionary<int, decimal> CalculateAllocations(
        List<DancerAllocationInputViewModel> selectedDancers,
        decimal netProfit,
        AllocationMode splitMode)
    {
        Dictionary<int, decimal> allocations =
            new Dictionary<int, decimal>();

        if (splitMode == AllocationMode.EqualSplit)
        {
            int count = selectedDancers.Count;
            decimal equalShare =
                Math.Floor((netProfit / count) * 100m) / 100m;

            if (equalShare <= 0m)
            {
                throw new InvalidOperationException(
                    "Net profit is too small to split equally.");
            }

            foreach (DancerAllocationInputViewModel item in selectedDancers)
            {
                allocations[item.DancerId] = equalShare;
            }

            return allocations;
        }

        foreach (DancerAllocationInputViewModel item in selectedDancers)
        {
            if (item.CustomCreditAmount <= 0m)
            {
                throw new InvalidOperationException(
                    $"Custom credit for {item.DancerName} must be > 0.");
            }

            allocations[item.DancerId] =
                Math.Round(item.CustomCreditAmount, 2);
        }

        return allocations;
    }

    /// <summary>
    /// Builds the parent running-total view model across all dancers
    /// linked to the specified parent user ID.
    /// </summary>
    public async Task<ParentFundraiserPortalViewModel>
        GetParentFundraiserSummaryAsync(string parentUserId)
    {
        List<Dancer> parentDancers = await _dbContext.Dancers
            .AsNoTracking()
            .Include((Dancer d) => d.Parent)
            .Include((Dancer d) => d.FundraiserCredits)
            .ThenInclude(
                (FundraiserLedgerTransaction t) => t.FundraiserEvent)
            .Where((Dancer d) =>
                d.Parent.UserId == parentUserId ||
                d.UserId == parentUserId)
            .OrderBy((Dancer d) => d.LastName)
            .ThenBy((Dancer d) => d.FirstName)
            .ToListAsync();

        ParentFundraiserPortalViewModel portalViewModel =
            new ParentFundraiserPortalViewModel();

        decimal familyRunningTotal = 0m;

        foreach (Dancer dancer in parentDancers)
        {
            DancerFundraiserSummaryViewModel dancerSummary =
                new DancerFundraiserSummaryViewModel
                {
                    DancerId = dancer.Id,
                    DancerName = dancer.FullName,
                    CurrentOutstandingBalance = dancer.OutstandingBalance
                };

            List<FundraiserLedgerTransaction> orderedCredits =
                dancer.FundraiserCredits
                    .OrderBy(
                        (FundraiserLedgerTransaction t) => t.TimestampUtc)
                    .ThenBy((FundraiserLedgerTransaction t) => t.Id)
                    .ToList();

            decimal dancerRunningTotal = 0m;

            foreach (FundraiserLedgerTransaction entry in orderedCredits)
            {
                dancerRunningTotal += entry.CreditAmount;

                string eventTitle = entry.FundraiserEvent != null
                    ? entry.FundraiserEvent.EventName
                    : "Fundraiser Credit";

                DateTime eventDate = entry.FundraiserEvent != null
                    ? entry.FundraiserEvent.EventDate
                    : entry.TimestampUtc;

                ParentLedgerItemViewModel itemVm =
                    new ParentLedgerItemViewModel
                    {
                        TransactionId = entry.Id,
                        FundraiserEventId = entry.FundraiserEventId,
                        EventName = eventTitle,
                        EventDate = eventDate,
                        CreditAmount = entry.CreditAmount,
                        RunningTotalAfterCredit = dancerRunningTotal,
                        BalanceAfterCredit = entry.NewBalance,
                        TimestampUtc = entry.TimestampUtc
                    };

                dancerSummary.CreditHistory.Add(itemVm);
            }

            dancerSummary.TotalFundraisingCreditsEarned =
                dancerRunningTotal;

            familyRunningTotal += dancerRunningTotal;
            portalViewModel.Dancers.Add(dancerSummary);
        }

        portalViewModel.FamilyRunningTotalCredits = familyRunningTotal;
        return portalViewModel;
    }
}
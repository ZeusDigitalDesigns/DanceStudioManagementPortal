using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EatonvilleDanceStudio.Data;
using EatonvilleDanceStudio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatonvilleDanceStudio.ViewComponents;

/// <summary>
/// Queries all featured competitions and renders a dynamic
/// branded announcement banner slideshow (#55).
/// </summary>
public class CompetitionBannersViewComponent(
    ApplicationDbContext context) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        DateTime cutoffDate = DateTime.Today.AddDays(-3);

        List<Competition> featuredBanners = await context
            .Competitions
            .Where(c => c.IsFeaturedAnnouncement
                        && (c.EndDate ?? c.StartDate) >= cutoffDate)
            .OrderBy(c => c.StartDate)
            .ToListAsync();

        return View(featuredBanners);
    }
}
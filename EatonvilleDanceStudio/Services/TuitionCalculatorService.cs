using System.Collections.Generic;
using System.Linq;
using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.Services;

public class TuitionCalculatorService : ITuitionCalculatorService
{
    public decimal CalculateMonthlyTuition(ParentProfile parent, List<Dancer> dancers)
    {
        decimal grossTuition = 0m;
        
        //calculate the raw gross tuition from all active classes
        foreach (Dancer dancer in dancers)
        {
            foreach (Enrollment enrollment in dancer.Enrollments)
            {
                if (enrollment.Status == "Enrolled" && enrollment.DanceClass != null)
                {
                    grossTuition += enrollment.DanceClass.MonthlyCost;
                }
            }
        }
        
        // If they don't have classes, bail early
        if (grossTuition == 0m) return 0m;
        
        // Calculate the Multi-Child Discount (Max 25%)
        decimal childDiscountRate = 0m;
        int activeChildCount = dancers.Count;
        
        if (activeChildCount == 2) childDiscountRate = 0.10m;
        else if (activeChildCount == 3) childDiscountRate = 0.15m;
        else if (activeChildCount == 4) childDiscountRate = 0.20m;
        else if (activeChildCount >= 5) childDiscountRate = 0.25m;

        // Apply child discount
        decimal discountedTuition = grossTuition * (1m - childDiscountRate);

        // 3. Apply Occupational Discount (10%)
        if (parent.IsMilitaryOrFirstResponder)
        {
            discountedTuition *= 0.90m;
        }

        // 4. Apply the $500 Monthly Cap
        if (discountedTuition > 500.00m)
        {
            discountedTuition = 500.00m;
        }

        return discountedTuition;

    }
}
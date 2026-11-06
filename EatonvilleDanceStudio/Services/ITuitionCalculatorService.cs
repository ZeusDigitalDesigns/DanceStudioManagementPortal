using System.Collections.Generic;
using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.Services;

public interface ITuitionCalculatorService
{
    decimal CalculateMonthlyTuition(ParentProfile parent, List<Dancer> dancers);
}
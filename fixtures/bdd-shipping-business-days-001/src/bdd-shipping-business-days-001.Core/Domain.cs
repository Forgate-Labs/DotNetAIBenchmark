namespace BddShippingBusinessDays001.Core;

public sealed class BusinessDayCalculator
{
    public DateOnly AddBusinessDays(DateOnly start, int days) => start.AddDays(days);
}

namespace TestsMoneyRounding001.Core;

public sealed class TaxCalculator
{
    public decimal CalculateTotal(IEnumerable<decimal> itemPrices, decimal taxRate)
    {
        var total = 0m;
        foreach (var price in itemPrices)
        {
            total += price + Math.Round(price * taxRate, 2, MidpointRounding.AwayFromZero);
        }
        return total;
    }
}

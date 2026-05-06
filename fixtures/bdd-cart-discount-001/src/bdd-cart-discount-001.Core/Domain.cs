namespace BddCartDiscount001.Core;

public sealed class CartCalculator
{
    public decimal CalculateTotal(decimal subtotal)
    {
        return subtotal > 100m ? subtotal * 0.9m : subtotal;
    }
}

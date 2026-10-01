using Xunit;

namespace Restaurant.Domain.Tests;

public class PosOrderServiceTests
{
    [Fact]
    public void FinancialCalculation_IsCorrect()
    {
        decimal subTotal = 200000;
        decimal discount = 50000;
        decimal tax = Math.Round(Math.Max(0, subTotal - discount) * 0.08m, 0);
        decimal finalAmount = Math.Max(0, subTotal - discount + tax);

        Assert.Equal(12000, tax);
        Assert.Equal(162000, finalAmount);
    }
}

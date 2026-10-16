using Invoice.BAL.Common;
using Invoice.Model;

namespace Invoice.BAL.Test;

public class LineCalculatorTests
{
    [Fact]
    public void Calculate_ShouldComputeGrossTaxAndTotal()
    {
        var result = LineCalculator.Calculate(10, 100, 0, 10);

        Assert.Equal(1000m, result.Gross);
        Assert.Equal(1000m, result.Taxable);
        Assert.Equal(100m, result.TaxAmount);
        Assert.Equal(1100m, result.LineTotal);
    }

    [Fact]
    public void Calculate_ShouldApplyDiscountBeforeTax()
    {
        var result = LineCalculator.Calculate(5, 200, 10, 10);

        Assert.Equal(1000m, result.Gross);
        Assert.Equal(990m, result.Taxable);
        Assert.Equal(99m, result.TaxAmount);
        Assert.Equal(1089m, result.LineTotal);
    }

    [Fact]
    public void Calculate_ShouldRoundHalfAwayFromZero()
    {
        // 3 x 33.335 = 100.005 -> 100.01 ; tax 7.5% of 100.01 = 7.50075 -> 7.50
        var result = LineCalculator.Calculate(3, 33.335m, 0, 7.5m);

        Assert.Equal(100.01m, result.Gross);
        Assert.Equal(7.50m, result.TaxAmount);
        Assert.Equal(107.51m, result.LineTotal);
    }

    [Fact]
    public void Calculate_ShouldReturnZeroTax_WhenTaxPercentIsZero()
    {
        var result = LineCalculator.Calculate(2, 50, 0, 0);

        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(100m, result.LineTotal);
    }

    [Fact]
    public void Round_ShouldUseTwoDecimalsAwayFromZero()
    {
        Assert.Equal(1.01m, LineCalculator.Round(1.005m));
        Assert.Equal(-1.01m, LineCalculator.Round(-1.005m));
    }

    [Fact]
    public void Validate_ShouldAcceptAValidLine()
    {
        LineCalculator.Validate(1, 1, 10, 0, 18);
    }

    [Theory]
    [InlineData(0, 10, 0, 0, "quantity")]
    [InlineData(-1, 10, 0, 0, "quantity")]
    [InlineData(1, -1, 0, 0, "rate")]
    [InlineData(1, 10, -1, 0, "discount")]
    [InlineData(1, 10, 0, -1, "tax")]
    [InlineData(1, 10, 0, 101, "tax")]
    [InlineData(1, 10, 11, 0, "discount")]
    public void Validate_ShouldThrow_ForInvalidLines(
        decimal quantity, decimal rate, decimal discount, decimal taxPercent, string expectedWord)
    {
        var ex = Assert.Throws<BusinessRuleException>(
            () => LineCalculator.Validate(3, quantity, rate, discount, taxPercent));

        Assert.Contains("Line 3", ex.Message);
        Assert.Contains(expectedWord, ex.Message);
    }
}

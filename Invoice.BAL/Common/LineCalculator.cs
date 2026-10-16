using Invoice.Model;

namespace Invoice.BAL.Common;

public readonly record struct LineAmounts(
    decimal Gross,
    decimal Taxable,
    decimal TaxAmount,
    decimal LineTotal);

/// <summary>
/// Single place for line maths so PO, Receipt and Sales Invoice always agree.
/// All money is rounded to 2 decimals, half away from zero.
/// </summary>
public static class LineCalculator
{
    public static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static LineAmounts Calculate(
        decimal quantity,
        decimal rate,
        decimal discountAmount,
        decimal taxPercent)
    {
        var gross = Round(quantity * rate);
        var taxable = gross - discountAmount;
        var tax = Round(taxable * taxPercent / 100m);

        return new LineAmounts(gross, taxable, tax, taxable + tax);
    }

    public static void Validate(
        int lineNumber,
        decimal quantity,
        decimal rate,
        decimal discountAmount,
        decimal taxPercent)
    {
        if (quantity <= 0)
            throw new BusinessRuleException($"Line {lineNumber}: quantity must be greater than zero.");

        if (rate < 0)
            throw new BusinessRuleException($"Line {lineNumber}: rate cannot be negative.");

        if (discountAmount < 0)
            throw new BusinessRuleException($"Line {lineNumber}: discount cannot be negative.");

        if (taxPercent < 0 || taxPercent > 100)
            throw new BusinessRuleException($"Line {lineNumber}: tax percent must be between 0 and 100.");

        if (discountAmount > Round(quantity * rate))
            throw new BusinessRuleException($"Line {lineNumber}: discount cannot exceed the line amount.");
    }
}

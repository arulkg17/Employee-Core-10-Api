namespace IMS.Blazor.Models;

public readonly record struct LineAmounts(decimal Gross, decimal Taxable, decimal TaxAmount, decimal LineTotal);

public readonly record struct Totals(decimal SubTotal, decimal TaxAmount, decimal TotalAmount);

/// <summary>Same maths as the server, so the screen shows what the API will save.</summary>
public static class Money
{
    public static decimal Round2(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static LineAmounts CalcLine(decimal quantity, decimal rate, decimal discountAmount, decimal taxPercent)
    {
        var gross = Round2(quantity * rate);
        var taxable = Round2(gross - discountAmount);
        var tax = Round2(taxable * taxPercent / 100m);

        return new LineAmounts(gross, taxable, tax, Round2(taxable + tax));
    }

    public static LineAmounts CalcLine(DocLine line) =>
        CalcLine(line.Quantity, line.Rate, line.DiscountAmount, line.TaxPercent);

    public static Totals Sum(IEnumerable<LineAmounts> lines)
    {
        var list = lines.ToList();
        var subTotal = Round2(list.Sum(l => l.Taxable));
        var tax = Round2(list.Sum(l => l.TaxAmount));

        return new Totals(subTotal, tax, Round2(subTotal + tax));
    }
}

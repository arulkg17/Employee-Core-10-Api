namespace IMS.Blazor.Models;

/// <summary>
/// One line of a Purchase Order or Sales Invoice, exactly what the API receives.
/// Tax amount and line total are calculated by the server.
/// </summary>
public class DocLine
{
    public int ItemmasterId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
}

public static class LineValidator
{
    /// <summary>Returns one message per problem; an empty list means the lines are fine.</summary>
    public static List<string> Validate(IReadOnlyList<DocLine> lines)
    {
        var errors = new List<string>();

        if (lines.Count == 0)
        {
            errors.Add("Add at least one line.");
            return errors;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var n = i + 1;

            if (line.ItemmasterId <= 0)
            {
                errors.Add($"Line {n}: select an item.");
            }

            if (line.Quantity <= 0)
            {
                errors.Add($"Line {n}: quantity must be above 0.");
            }

            if (line.Rate < 0)
            {
                errors.Add($"Line {n}: rate cannot be negative.");
            }

            if (line.DiscountAmount < 0 || line.DiscountAmount > Money.Round2(line.Quantity * line.Rate))
            {
                errors.Add($"Line {n}: discount must be between 0 and quantity x rate.");
            }

            if (line.TaxPercent < 0 || line.TaxPercent > 100)
            {
                errors.Add($"Line {n}: tax % must be between 0 and 100.");
            }
        }

        return errors;
    }
}

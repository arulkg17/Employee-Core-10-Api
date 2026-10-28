using Invoice.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection.Metadata;

public class DocumentPdfService : IDocumentPdfService
{
    private record PdfLine(string Item, decimal Qty, decimal Rate, decimal Discount,
                           decimal TaxPercent, decimal TaxAmount, decimal LineTotal);

    private record PdfDoc(string Title, string Number, DateTime Date, string Status,
                          string PartyLabel, string PartyName,
                          List<(string Label, string Value)> Extra,
                          string? Notes, decimal SubTotal, decimal Tax, decimal Total,
                          List<PdfLine> Lines, string? Watermark);

    public byte[] GeneratePurchaseOrder(PurchaseOrderDto po, string vendorName,
        IReadOnlyDictionary<int, string> itemNames)
    {
        var lines = po.Details.Select(d => new PdfLine(
            Name(itemNames, d.ItemmasterId), d.Quantity, d.Rate, d.DiscountAmount,
            d.TaxPercent, d.TaxAmount, d.LineTotal)).ToList();

        string? mark = po.Status switch
        {
            "Draft" => "DRAFT - PENDING APPROVAL",
            "Cancelled" => "CANCELLED",
            _ => null
        };

        return Build(new PdfDoc("PURCHASE ORDER", po.PONumber, po.PODate, po.Status,
            "Vendor", vendorName, new(), po.Notes,
            po.SubTotal, po.TaxAmount, po.TotalAmount, lines, mark));
    }

    public byte[] GenerateSalesInvoice(SalesInvoiceDto inv, string customerName,
        IReadOnlyDictionary<int, string> itemNames)
    {
        var lines = inv.Details.Select(d => new PdfLine(
            Name(itemNames, d.ItemmasterId), d.Quantity, d.Rate, d.DiscountAmount,
            d.TaxPercent, d.TaxAmount, d.LineTotal)).ToList();

        var extra = new List<(string, string)>();
        if (inv.DueDate.HasValue) extra.Add(("Due Date", inv.DueDate.Value.ToString("dd-MMM-yyyy")));

        string? mark = inv.Status switch
        {
            "Draft" => "DRAFT",
            "Cancelled" => "CANCELLED",
            _ => null
        };

        return Build(new PdfDoc("SALES INVOICE", inv.InvoiceNumber ?? "(not generated)",
            inv.InvoiceDate, inv.Status, "Customer", customerName, extra, inv.Notes,
            inv.SubTotal, inv.TaxAmount, inv.TotalAmount, lines, mark));
    }

    public byte[] GenerateReceipt(ReceiptDto rc, string vendorName, string poNumber,
        IReadOnlyDictionary<int, string> itemNames)
    {
        var lines = rc.Details.Select(d => new PdfLine(
            Name(itemNames, d.ItemmasterId), d.ReceivedQuantity, d.Rate, d.DiscountAmount,
            d.TaxPercent, d.TaxAmount, d.LineTotal)).ToList();

        string? mark = rc.Status switch
        {
            "Draft" => "DRAFT",
            "Cancelled" => "CANCELLED",
            _ => null
        };

        return Build(new PdfDoc("GOODS RECEIPT", rc.ReceiptNumber ?? "(not generated)",
            rc.ReceiptDate, rc.Status, "Vendor", vendorName,
            new() { ("PO No", poNumber) }, rc.Notes,
            rc.SubTotal, rc.TaxAmount, rc.TotalAmount, lines, mark));
    }

    private static string Name(IReadOnlyDictionary<int, string> map, int id)
        => map.TryGetValue(id, out var n) ? n : $"Item #{id}";

    private static byte[] Build(PdfDoc m)
    {
        return QuestPDF.Fluent.Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(t => t.FontSize(10));

            if (m.Watermark != null)
                page.Background().AlignCenter().AlignMiddle().Rotate(-35)
                    .Text(m.Watermark).FontSize(44).Bold().FontColor(Colors.Grey.Lighten2);

            page.Header().Column(h =>
            {
                h.Item().Text("Your Company Name").FontSize(16).Bold();
                h.Item().PaddingBottom(5).LineHorizontal(1);
                h.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text(m.Title).FontSize(14).Bold();
                        c.Item().Text($"No: {m.Number}");
                        c.Item().Text($"Date: {m.Date:dd-MMM-yyyy}");
                        c.Item().Text($"Status: {m.Status}");
                        foreach (var (label, value) in m.Extra)
                            c.Item().Text($"{label}: {value}");
                    });
                    r.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text(m.PartyLabel).SemiBold();
                        c.Item().Text(m.PartyName);
                    });
                });
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(25);   // #
                        c.RelativeColumn();     // Item
                        c.ConstantColumn(50);   // Qty
                        c.ConstantColumn(55);   // Rate
                        c.ConstantColumn(55);   // Discount
                        c.ConstantColumn(40);   // Tax %
                        c.ConstantColumn(55);   // Tax
                        c.ConstantColumn(65);   // Total
                    });

                    t.Header(h =>
                    {
                        void H(string s, bool right = true)
                        {
                            var cell = h.Cell().Background(Colors.Grey.Lighten3).Padding(4);
                            (right ? cell.AlignRight() : cell).Text(s).SemiBold();
                        }
                        H("#", false); H("Item", false); H("Qty"); H("Rate");
                        H("Disc."); H("Tax %"); H("Tax"); H("Total");
                    });

                    int i = 1;
                    foreach (var l in m.Lines)
                    {
                        void C(string s, bool right = true)
                        {
                            var cell = t.Cell().BorderBottom(0.5f)
                                        .BorderColor(Colors.Grey.Lighten1).Padding(4);
                            (right ? cell.AlignRight() : cell).Text(s);
                        }
                        C((i++).ToString(), false); C(l.Item, false);
                        C(l.Qty.ToString("N2")); C(l.Rate.ToString("N2"));
                        C(l.Discount.ToString("N2")); C(l.TaxPercent.ToString("N1"));
                        C(l.TaxAmount.ToString("N2")); C(l.LineTotal.ToString("N2"));
                    }
                });

                col.Item().PaddingTop(10).AlignRight().Column(tot =>
                {
                    tot.Item().Text($"Sub Total: {m.SubTotal:N2}");
                    tot.Item().Text($"Tax: {m.Tax:N2}");
                    tot.Item().Text($"Total: {m.Total:N2}").FontSize(12).Bold();
                });

                if (!string.IsNullOrWhiteSpace(m.Notes))
                    col.Item().PaddingTop(10).Text($"Notes: {m.Notes}").Italic();
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Page "); x.CurrentPageNumber();
                x.Span(" of "); x.TotalPages();
            });
        })).GeneratePdf();
    }
}
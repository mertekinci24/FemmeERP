using InventoryERP.Domain.Entities;

namespace InventoryERP.Domain.Services;

public static class DocumentCalculator
{
    public record Totals(decimal Net, decimal Vat, decimal Gross);

    public static Totals ComputeTotals(IEnumerable<DocumentLine> lines)
    {
        decimal net = 0, vat = 0, gross = 0;
        foreach (var line in lines)
        {
            var lineNet = line.Qty * line.UnitPrice;
            var lineVat = lineNet * line.VatRate / 100m;
            var lineGross = lineNet + lineVat;
            net += lineNet;
            vat += lineVat;
            gross += lineGross;
        }
        return new Totals(Math.Round(net, 2), Math.Round(vat, 2), Math.Round(gross, 2));
    }
}

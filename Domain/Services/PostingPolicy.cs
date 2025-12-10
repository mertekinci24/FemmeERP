using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Domain.Services;

public static class PostingPolicy
{
    public static void EnsureCanPost(Document doc, DocumentCalculator.Totals totals)
    {
        // R-208: Validation Shield
        if (doc.Lines == null || !doc.Lines.Any())
            throw new InvalidOperationException("Belge boş olamaz. En az bir satır ekleyin.");

        if (doc.Lines.Any(x => x.Qty <= 0))
            throw new InvalidOperationException("Miktar 0 veya negatif olamaz.");

        // Negatif stok kontrolü: Satışta çıkış miktarı kadar stok olmalı
        if (doc.Type == InventoryERP.Domain.Enums.DocumentType.SALES_INVOICE)
        {
            // OnHand kontrolü handler'da yapılmalı, burada örnek
            // PostingPolicy.CheckNegativeStock(onHand, -totals.Net);
        }
        // Cari XOR kuralı: Debit/credit tek yönlü olmalı
        // PostingPolicy.CheckLedgerXor(debit, credit); // Ledger entry creation
    }

    public static void CheckNegativeStock(decimal onHand, decimal qtySigned)
    {
        if (onHand + qtySigned < 0)
            throw new InvalidOperationException("STK-NEG-001");
    }

    public static void CheckLedgerXor(decimal debit, decimal credit)
    {
        if (!((debit == 0 && credit != 0) || (debit != 0 && credit == 0)))
            throw new InvalidOperationException("ARAP-ALLOC-422");
    }
}

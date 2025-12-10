using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class PaymentAllocation : EntityBase
{
    public int PaymentEntryId { get; set; }
    public int InvoiceEntryId { get; set; }
    public decimal AmountTry { get; set; }

    public PartnerLedgerEntry? PaymentEntry { get; set; }
    public PartnerLedgerEntry? InvoiceEntry { get; set; }
    // Allocation rules: AmountTry > 0, same partner, sum(allocation) <= entry.AmountTry
}


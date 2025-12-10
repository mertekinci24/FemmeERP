namespace InventoryERP.Domain.Enums;

public enum DocumentType
{
    SALES_INVOICE = 1,
    PURCHASE_INVOICE = 2,
    SALES_ORDER = 10,
    RECEIPT = 3,              // R-007: Cash/Bank receipt (Tahsilat Fişi)
    DISBURSEMENT = 4,         // Legacy: Disbursement
    PAYMENT = 4,              // R-007: Cash/Bank payment (Ödeme Fişi) - alias for DISBURSEMENT
    RCPT_CUSTOMER = 5,
    PMT_SUPPLIER = 6
    ,
    SEVK_IRSALIYESI = 7,
    GELEN_IRSALIYE = 8,
    SAYIM_FISI = 9,
    TRANSFER_FISI = 11,
    URETIM_FISI = 12,
    ADJUSTMENT_IN = 13,
    ADJUSTMENT_OUT = 14,
    QUOTE = 15               // R-060: Quote document (no stock/ledger posting)
}


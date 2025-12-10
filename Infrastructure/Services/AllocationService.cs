using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Services;

public sealed class AllocationService
{
    private readonly AppDbContext _db;
    public AllocationService(AppDbContext db) => _db = db;

    // Allocates payment to invoice
    public async Task AllocateAsync(int paymentEntryId, int invoiceEntryId, decimal amountTry, CancellationToken ct = default)
    {
        var payment = await _db.PartnerLedgerEntries.FindAsync(new object[] { paymentEntryId }, ct) ?? throw new InvalidOperationException("PAYMENT-404");
        var invoice = await _db.PartnerLedgerEntries.FindAsync(new object[] { invoiceEntryId }, ct) ?? throw new InvalidOperationException("INVOICE-404");
        if (payment.PartnerId != invoice.PartnerId) throw new InvalidOperationException("PARTNER-MISMATCH");
        if (amountTry <= 0 || amountTry > payment.AmountTry) throw new InvalidOperationException("ALLOC-AMOUNT-INVALID");
        var totalAlloc = (await _db.PaymentAllocations
            .Where(a => a.PaymentEntryId == paymentEntryId)
            .Select(a => a.AmountTry)
            .ToListAsync(ct))
            .Sum();
        if (totalAlloc + amountTry > payment.AmountTry) throw new InvalidOperationException("ALLOC-OVERFLOW");
        _db.PaymentAllocations.Add(new PaymentAllocation {
            PaymentEntryId = paymentEntryId,
            InvoiceEntryId = invoiceEntryId,
            AmountTry = amountTry
        });
        await _db.SaveChangesAsync(ct);

        // Update statuses: if invoice is fully allocated, mark it CLOSED. If payment fully allocated, mark it CLOSED.
        var invoiceTotalAllocated = (await _db.PaymentAllocations
            .Where(a => a.InvoiceEntryId == invoiceEntryId)
            .Select(a => a.AmountTry)
            .ToListAsync(ct))
            .Sum();
        if (invoiceTotalAllocated >= invoice.AmountTry)
        {
            invoice.Status = InventoryERP.Domain.Enums.LedgerStatus.CLOSED;
        }

        var paymentTotalAllocated = (await _db.PaymentAllocations
            .Where(a => a.PaymentEntryId == paymentEntryId)
            .Select(a => a.AmountTry)
            .ToListAsync(ct))
            .Sum();
        if (paymentTotalAllocated >= payment.AmountTry)
        {
            payment.Status = InventoryERP.Domain.Enums.LedgerStatus.CLOSED;
        }

        await _db.SaveChangesAsync(ct);
    }

    // Deallocates payment from invoice
    public async Task DeallocateAsync(int allocationId, CancellationToken ct = default)
    {
        var alloc = await _db.PaymentAllocations.FindAsync(new object[] { allocationId }, ct) ?? throw new InvalidOperationException("ALLOC-404");
        var payment = await _db.PartnerLedgerEntries.FindAsync(new object[] { alloc.PaymentEntryId }, ct) ?? throw new InvalidOperationException("PAYMENT-404");
        var invoice = await _db.PartnerLedgerEntries.FindAsync(new object[] { alloc.InvoiceEntryId }, ct) ?? throw new InvalidOperationException("INVOICE-404");
        _db.PaymentAllocations.Remove(alloc);
        await _db.SaveChangesAsync(ct);

        // After deallocation, ensure statuses reflect remaining allocations
        var invoiceAlloc = (await _db.PaymentAllocations
            .Where(a => a.InvoiceEntryId == invoice.Id)
            .Select(a => a.AmountTry)
            .ToListAsync(ct))
            .Sum();
        invoice.Status = invoiceAlloc >= invoice.AmountTry ? InventoryERP.Domain.Enums.LedgerStatus.CLOSED : InventoryERP.Domain.Enums.LedgerStatus.OPEN;

        var paymentAlloc = (await _db.PaymentAllocations
            .Where(a => a.PaymentEntryId == payment.Id)
            .Select(a => a.AmountTry)
            .ToListAsync(ct))
            .Sum();
        payment.Status = paymentAlloc >= payment.AmountTry ? InventoryERP.Domain.Enums.LedgerStatus.CLOSED : InventoryERP.Domain.Enums.LedgerStatus.OPEN;

        await _db.SaveChangesAsync(ct);
    }

    // Auto-allocate oldest open invoices for a partner up to amount hint
    public async Task<int> AutoAllocateOldestAsync(int partnerId, decimal? amountTryHint, CancellationToken ct = default)
    {
        var payments = await _db.PartnerLedgerEntries
            .Where(e => e.PartnerId == partnerId && e.Status == LedgerStatus.OPEN && e.Credit > 0)
            .OrderBy(e => e.Date)
            .ToListAsync(ct);
        var invoices = await _db.PartnerLedgerEntries
            .Where(e => e.PartnerId == partnerId && e.Status == LedgerStatus.OPEN && e.Debit > 0)
            .OrderBy(e => e.Date)
            .ToListAsync(ct);
        decimal remaining = amountTryHint ?? payments.Sum(p => p.AmountTry);
        int allocCount = 0;
        foreach (var payment in payments)
        {
            foreach (var invoice in invoices)
            {
                if (remaining <= 0) break;
                var allocatable = Math.Min(payment.AmountTry, invoice.AmountTry);
                var toAlloc = Math.Min(allocatable, remaining);
                if (toAlloc > 0)
                {
                    _db.PaymentAllocations.Add(new PaymentAllocation {
                        PaymentEntryId = payment.Id,
                        InvoiceEntryId = invoice.Id,
                        AmountTry = toAlloc
                    });
                    remaining -= toAlloc;
                    allocCount++;
                }
            }
            if (remaining <= 0) break;
        }
        await _db.SaveChangesAsync(ct);
        return allocCount;
    }
}

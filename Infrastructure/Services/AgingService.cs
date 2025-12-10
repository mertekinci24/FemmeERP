using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence;

public sealed class PartnerAgingDto {
    public int PartnerId { get; set; }
    public Dictionary<string, decimal> Buckets { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime AsOf { get; set; }
}

public sealed class AgingService {
    private readonly AppDbContext _db;
    public AgingService(AppDbContext db) => _db = db;

    private static decimal Remaining(PartnerLedgerEntry e, IEnumerable<PaymentAllocation> allocs) {
        var used = e.Debit > 0
            ? allocs.Where(a => a.InvoiceEntryId == e.Id).Sum(a => a.AmountTry)
            : allocs.Where(a => a.PaymentEntryId == e.Id).Sum(a => a.AmountTry);
        return Math.Round(e.AmountTry - used, 2, MidpointRounding.AwayFromZero);
    }
    private static string Bucket(DateTime due, DateTime today) {
        var d = (today.Date - due.Date).Days;
        if (d < 0) return "NotDue";
        if (d <= 30) return "0-30";
        if (d <= 60) return "31-60";
        if (d <= 90) return "61-90";
        return "90+";
    }

    public async Task<PartnerAgingDto> GetPartnerAgingAsync(int partnerId, DateTime? asOf = null, CancellationToken ct = default) {
        var today = (asOf ?? DateTime.UtcNow).Date;
        var entries = await _db.PartnerLedgerEntries
            .Where(e => e.PartnerId == partnerId && e.Status == LedgerStatus.OPEN)
            .Select(e => new { e.Id, e.Date, Due = (DateTime?)(e.DueDate ?? e.Date), e.Debit, e.Credit, e.AmountTry })
            .ToListAsync(ct);
        var allocs = await _db.PaymentAllocations
            .Where(a => entries.Select(x => x.Id).Contains(a.PaymentEntryId) || entries.Select(x => x.Id).Contains(a.InvoiceEntryId))
            .ToListAsync(ct);
        var lines = entries.Select(e => {
            var remaining = Remaining(
                new PartnerLedgerEntry { Id = e.Id, Debit = e.Debit, Credit = e.Credit, AmountTry = e.AmountTry },
                allocs);
            var bucket = Bucket(e.Due!.Value, today);
            var sign = e.Debit > 0 ? +1 : -1;
            return new { e.Id, Remaining = remaining * sign, Bucket = bucket };
        })
        .Where(x => x.Remaining != 0m)
        .ToList();
        var groups = lines.GroupBy(x => x.Bucket)
            .ToDictionary(g => g.Key, g => Math.Round(g.Sum(z => z.Remaining), 2));
        var total = Math.Round(lines.Sum(x => x.Remaining), 2);
        return new PartnerAgingDto { PartnerId = partnerId, Buckets = groups, Total = total, AsOf = today };
    }
}

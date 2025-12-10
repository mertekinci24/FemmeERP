using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Persistence;
using InventoryERP.Application.Reports;

namespace InventoryERP.Infrastructure.Queries;

public sealed class DashboardQueriesEf : IDashboardQueries
{
    private readonly AppDbContext _db;
    public DashboardQueriesEf(AppDbContext db) => _db = db;

    public async Task<ThisMonthSalesContract> GetThisMonthSalesAsync(int year, int month, CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddTicks(-1);

        var q = _db.Documents.AsNoTracking()
            .Where(d => d.Type == InventoryERP.Domain.Enums.DocumentType.SALES_INVOICE && d.Status == InventoryERP.Domain.Enums.DocumentStatus.POSTED && d.Date >= start && d.Date <= end);

    var totals = await q.Select(d => d.TotalTry).ToListAsync(ct);
    var total = totals.Sum();
    var count = totals.Count;
        return new ThisMonthSalesContract(year, month, total, count);
    }

    public async Task<OverdueReceivablesContract> GetOverdueReceivablesAsync(DateTime? asOf = null, int topN = 5, CancellationToken ct = default)
    {
        var today = (asOf ?? DateTime.UtcNow).Date;
        // consider customers only (BOTH deprecated in R-086)
        var partners = await _db.Partners.Where(p => p.PartnerType == InventoryERP.Domain.Enums.PartnerType.Customer).ToListAsync(ct);
        var agingSvc = new AgingService(_db);
        var list = new List<OverduePartnerDto>();
        decimal totalOverdue = 0m;
        foreach (var p in partners)
        {
            var aging = await agingSvc.GetPartnerAgingAsync(p.Id, asOf, ct);
            if (aging.Total > 0m)
            {
                totalOverdue += aging.Total;
                list.Add(new OverduePartnerDto(p.Id, p.Name, aging.Total));
            }
        }
        var top = list.OrderByDescending(x => x.TotalOverdue).Take(topN).ToList();
        return new OverdueReceivablesContract(Math.Round(totalOverdue, 2), list.Count, top);
    }

    public async Task<CashBankBalancesContract> GetCashBankBalancesAsync(DateTime? asOf = null, CancellationToken ct = default)
    {
        var asOfDate = (asOf ?? DateTime.UtcNow).Date;

        // Select partners explicitly marked as cash or bank accounts (now use PartnerType.Other in R-086)
        var candidates = await _db.Partners.AsNoTracking()
            .Where(p => p.PartnerType == InventoryERP.Domain.Enums.PartnerType.Other)
            .ToListAsync(ct);

        var list = new List<CashAccountBalanceDto>();
        decimal total = 0m;
        foreach (var p in candidates)
        {
            var entries = await _db.PartnerLedgerEntries.AsNoTracking()
                .Where(e => e.PartnerId == p.Id && e.Date <= asOfDate && e.Status != InventoryERP.Domain.Enums.LedgerStatus.CANCELED)
                .Select(e => e.AmountTry)
                .ToListAsync(ct);
            var sum = entries.Sum();
            if (Math.Abs(sum) > 0m)
            {
                list.Add(new CashAccountBalanceDto(p.Id, p.Name, Math.Round(sum, 2)));
                total += sum;
            }
        }

        return new CashBankBalancesContract(Math.Round(total, 2), list);
    }

    public async Task<TopProductsContract> GetTopProductsAsync(DateTime? from = null, DateTime? to = null, int topN = 10, CancellationToken ct = default)
    {
        var fromDate = (from ?? DateTime.MinValue).Date;
        var toDate = (to ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var q = from dl in _db.DocumentLines.AsNoTracking()
                join d in _db.Documents.AsNoTracking() on dl.DocumentId equals d.Id
                join p in _db.Products.AsNoTracking() on dl.ItemId equals p.Id
                where d.Type == InventoryERP.Domain.Enums.DocumentType.SALES_INVOICE && d.Status == InventoryERP.Domain.Enums.DocumentStatus.POSTED
                      && d.Date >= fromDate && d.Date <= toDate
                select new { dl.ItemId, p.Sku, p.Name, Qty = dl.Qty, Fx = d.FxRate, UnitPrice = dl.UnitPrice };

        var rows = await q.ToListAsync(ct);

        var ag = rows.GroupBy(x => new { x.ItemId, x.Sku, x.Name })
            .Select(g => new TopProductDto(
                g.Key.ItemId,
                g.Key.Sku,
                g.Key.Name,
                g.Sum(x => x.Qty),
                g.Sum(x => x.UnitPrice * x.Qty * (x.Fx ?? 1m))
            ))
            .OrderByDescending(t => t.RevenueTry)
            .ThenByDescending(t => t.Quantity)
            .Take(topN)
            .ToList();

        return new TopProductsContract(ag);
    }
}

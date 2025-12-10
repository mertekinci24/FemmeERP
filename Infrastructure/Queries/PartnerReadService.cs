using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Partners;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Queries;

public sealed class PartnerReadService : IPartnerReadService
{
    private readonly AppDbContext _db;
    public PartnerReadService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PartnerRowDto>> GetListAsync(string? search, int page = 1, int pageSize = 100)
    {
        var q = _db.Partners.AsNoTracking().Where(p => !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) ||
                              (p.TaxId != null && p.TaxId.Contains(search)) ||
                              (p.NationalId != null && p.NationalId.Contains(search)));
        var partners = await q
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var partnerIds = partners.Select(x => x.Id).ToList();
        List<PartnerLedgerProjection> ledgerEntries;
        if (partnerIds.Count == 0)
        {
            ledgerEntries = new List<PartnerLedgerProjection>();
        }
        else
        {
            ledgerEntries = await _db.PartnerLedgerEntries
                .Where(e => partnerIds.Contains(e.PartnerId))
                .Select(e => new PartnerLedgerProjection(
                    e.PartnerId,
                    e.AmountTry,
                    e.Debit,
                    e.Credit,
                    e.Date,
                    e.DocNumber,
                    e.DocType,
                    e.Id
                ))
                .ToListAsync();
        }

        var summaryLookup = ledgerEntries
            .GroupBy(e => e.PartnerId)
            .ToDictionary(g => g.Key, g => new
            {
                // R-219 FIX: Balance = Sum(Debit) - Sum(Credit), NOT Sum(AmountTry)
                // Debit = money owed TO us (customer owes us) or we PAID vendor
                // Credit = money owed BY us (we owe vendor) or customer PAID us
                Balance = g.Sum(x => x.Debit) - g.Sum(x => x.Credit),
                TotalDebit = g.Sum(x => x.Debit),
                TotalCredit = g.Sum(x => x.Credit),
                Last = g.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).FirstOrDefault()
            });

        return partners.Select(p =>
        {
            summaryLookup.TryGetValue(p.Id, out var summary);
            var last = summary?.Last;
            return new PartnerRowDto
            {
                Id = p.Id,
                Name = p.Name,
                Role = p.PartnerType.ToString(),
                TaxNo = p.TaxId ?? string.Empty,
                NationalId = p.NationalId,
                Email = p.Email,
                Phone = p.Phone,
                IsActive = p.IsActive,
                BalanceTry = summary?.Balance ?? 0m,
                CreditLimitTry = p.CreditLimitTry,
                TotalDebit = summary?.TotalDebit ?? 0m,
                TotalCredit = summary?.TotalCredit ?? 0m,
                LastMovementDate = last?.Date,
                LastDocNumber = last?.DocNumber,
                LastDocType = last?.DocType?.ToString(),
                LastDebit = last?.Debit,
                LastCredit = last?.Credit,
                LastBalanceAfter = summary?.Balance ?? 0m
            };
        }).ToList();
    }

    public async Task<int> GetTotalCountAsync(string? search)
    {
        var q = _db.Partners.AsNoTracking().Where(p => !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) ||
                              (p.TaxId != null && p.TaxId.Contains(search)) ||
                              (p.NationalId != null && p.NationalId.Contains(search)));
        return await q.CountAsync();
    }

    public async Task<PartnerDetailDto?> GetDetailAsync(int id)
    {
        var p = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return null;
        // R-219 FIX: Balance = Sum(Debit) - Sum(Credit), NOT Sum(AmountTry)
        var balEntries = await _db.PartnerLedgerEntries
            .Where(e => e.PartnerId == id)
            .GroupBy(e => 1)
            .Select(g => new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync();
        var bal = (balEntries?.Debit ?? 0m) - (balEntries?.Credit ?? 0m);
        return new PartnerDetailDto(p.Id, p.Name, p.PartnerType.ToString(), p.TaxId ?? "", bal, p.CreditLimitTry);
    }

    public async Task<StatementDto> BuildStatementAsync(int partnerId, DateOnly? from, DateOnly? to)
    {
        var baseQuery = _db.PartnerLedgerEntries
            .AsNoTracking()
            .Where(p => p.PartnerId == partnerId && p.Status != LedgerStatus.CANCELED);

        decimal openingBalance = 0m;
        decimal openingDebit = 0m;
        decimal openingCredit = 0m;

        if (from is not null)
        {
            var fromDt = from.Value.ToDateTime(new TimeOnly(0, 0));
            // Calculate Opening Balance (Devir)
            var openingEntries = await baseQuery
                .Where(p => p.Date < fromDt)
                .GroupBy(p => 1)
                .Select(g => new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
                .FirstOrDefaultAsync();
            
            if (openingEntries != null)
            {
                openingDebit = openingEntries.Debit;
                openingCredit = openingEntries.Credit;
                openingBalance = openingDebit - openingCredit;
            }
        }

        var query = baseQuery;
        if (from is not null)
        {
            var fromDt = from.Value.ToDateTime(new TimeOnly(0, 0));
            query = query.Where(p => p.Date >= fromDt);
        }
        if (to is not null)
        {
            var toDt = to.Value.ToDateTime(new TimeOnly(23, 59, 59));
            query = query.Where(p => p.Date <= toDt);
        }

        var rows = await query.OrderBy(p => p.Date).ThenBy(p => p.Id).ToListAsync();
        var dtoRows = new List<PartnerStatementRowDto>();

        // Add Opening Balance Row if applicable
        if (from is not null || openingBalance != 0)
        {
            string dir = openingBalance > 0 ? "B" : (openingBalance < 0 ? "A" : "-");
            dtoRows.Add(new PartnerStatementRowDto(
                from?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue, 
                "DEVİR", 
                "-", 
                "Devir Bakiyesi", 
                openingDebit, 
                openingCredit, 
                0, 
                Math.Abs(openingBalance), 
                dir
            ));
        }

        decimal running = openingBalance;
        foreach (var r in rows)
        {
            running += r.Debit - r.Credit;
            string dir = running > 0 ? "B" : (running < 0 ? "A" : "-");
            string desc = GetDescription(r.DocType);
            
            dtoRows.Add(new PartnerStatementRowDto(
                r.Date, 
                r.DocType?.ToString() ?? "", 
                r.DocNumber ?? "", 
                desc,
                r.Debit, 
                r.Credit, 
                r.AmountTry, 
                Math.Abs(running), 
                dir
            ));
        }

        var totalDebit = openingDebit + rows.Sum(x => x.Debit);
        var totalCredit = openingCredit + rows.Sum(x => x.Credit);
        
        return new StatementDto(dtoRows, totalDebit, totalCredit, running);
    }

    private string GetDescription(DocumentType? type)
    {
        return type switch
        {
            DocumentType.SALES_INVOICE => "Satış Faturası",
            DocumentType.PURCHASE_INVOICE => "Alış Faturası",
            DocumentType.PAYMENT => "Ödeme",
            DocumentType.PMT_SUPPLIER => "Ödeme",
            DocumentType.RECEIPT => "Tahsilat",
            DocumentType.RCPT_CUSTOMER => "Tahsilat",
            _ => type?.ToString() ?? "İşlem"
        };
    }

    public async Task<AgingDto> BuildAgingAsync(int partnerId, DateOnly asOf)
    {
        var asOfDt = asOf.ToDateTime(new TimeOnly(23, 59, 59));
        var entries = await _db.PartnerLedgerEntries
            .AsNoTracking()
            .Where(p => p.PartnerId == partnerId && p.Status == LedgerStatus.OPEN)
            .ToListAsync();

        decimal b0 = 0m, b30 = 0m, b60 = 0m, b90 = 0m;
        foreach (var e in entries)
        {
            if (e.DueDate is null) continue;
            var age = (asOfDt - e.DueDate.Value).TotalDays;
            var amount = e.Debit - e.Credit;
            if (age <= 30) b0 += amount;
            else if (age <= 60) b30 += amount;
            else if (age <= 90) b60 += amount;
            else b90 += amount;
        }

        var buckets = new List<AgingBucketDto>
        {
            new("0-30", b0),
            new("31-60", b30),
            new("61-90", b60),
            new("90+", b90)
        };

        return new AgingDto(buckets, buckets.Sum(b => b.AmountTry));
    }
}

file sealed record PartnerLedgerProjection(int PartnerId, decimal AmountTry, decimal Debit, decimal Credit, DateTime Date,
    string? DocNumber, DocumentType? DocType, int Id);

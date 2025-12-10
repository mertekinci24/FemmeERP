using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Queries;

public sealed class DocumentQueries : IDocumentQueries
{
    private readonly AppDbContext _db;
    public DocumentQueries(AppDbContext db) => _db = db;

    public async Task<InventoryERP.Application.Documents.PagedResult<InventoryERP.Application.Documents.DocumentRowDto>> ListAsync(InventoryERP.Application.Documents.DTOs.DocumentListFilter filter, int page, int pageSize)
    {
    var q = _db.Documents.Include(d => d.Partner).AsNoTracking();
        if (filter.DateFrom is not null) q = q.Where(d => d.Date >= filter.DateFrom);
        if (filter.DateTo is not null) q = q.Where(d => d.Date <= filter.DateTo);
        if (!string.IsNullOrWhiteSpace(filter.SearchText)) q = q.Where(d => (d.Number ?? "").Contains(filter.SearchText));
    if (TryParseDocumentType(filter.Type, out var docType)) q = q.Where(d => d.Type == docType);
    if (TryParseDocumentStatus(filter.Status, out var docStatus)) q = q.Where(d => d.Status == docStatus);
        if (filter.PartnerId is not null) q = q.Where(d => d.PartnerId == filter.PartnerId);

        // compute total
        var total = await q.CountAsync();

        // ordering
        if (!string.IsNullOrWhiteSpace(filter.SortBy))
        {
            // only a few fields supported
            var dirDesc = string.Equals(filter.SortDir, "DESC", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(filter.SortBy, "Date", StringComparison.OrdinalIgnoreCase))
                q = dirDesc ? q.OrderByDescending(d => d.Date) : q.OrderBy(d => d.Date);
            else if (string.Equals(filter.SortBy, "Number", StringComparison.OrdinalIgnoreCase))
                q = dirDesc ? q.OrderByDescending(d => d.Number) : q.OrderBy(d => d.Number);
            else
                q = q.OrderByDescending(d => d.Date);
        }
        else
        {
            q = q.OrderByDescending(d => d.Date);
        }

    page = Math.Max(1, page);
    pageSize = Math.Max(1, pageSize);

        var pageDocs = await q
            .Include(d => d.Lines)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();

        // R-249: Query warehouse name using separate lookup to avoid complex join
        var warehouseIds = pageDocs.Where(d => d.SourceWarehouseId.HasValue).Select(d => d.SourceWarehouseId!.Value).Distinct().ToList();
        var warehouseNames = warehouseIds.Count > 0 
            ? await _db.Warehouses.Where(w => warehouseIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, w => w.Name)
            : new Dictionary<int, string>();

        var items = pageDocs
            .Select(d => new InventoryERP.Application.Documents.DocumentRowDto(
                d.Id,
                d.Number ?? string.Empty,
                d.Type.ToString(),
                d.Date,
                d.Partner != null ? d.Partner.Name : string.Empty,
                d.Status.ToString(),
                d.Lines.Sum(l => l.Qty * l.UnitPrice),
                d.Lines.Sum(l => l.Qty * l.UnitPrice * l.VatRate / 100m),
                d.TotalTry,
                // R-249: Add WarehouseName and Description for list columns
                d.SourceWarehouseId.HasValue && warehouseNames.ContainsKey(d.SourceWarehouseId.Value) ? warehouseNames[d.SourceWarehouseId.Value] : null,
                d.Description))
            .ToList();

        return new InventoryERP.Application.Documents.PagedResult<InventoryERP.Application.Documents.DocumentRowDto>(items, total);
    }

    public async Task<InventoryERP.Application.Documents.DTOs.DocumentDetailDto?> GetAsync(int id)
    {
        var d = await _db.Documents
            .Include(x => x.Lines).ThenInclude(l => l.Item)  // R-045: Include Item to get ItemName
            .Include(x => x.Partner)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return null;
        var dto = new InventoryERP.Application.Documents.DTOs.DocumentDetailDto
        {
            Id = d.Id,
            Type = d.Type.ToString(),
            Number = d.Number ?? string.Empty,
            Date = d.Date,
            // R-109: Preserve null PartnerId instead of converting to 0 so UI logic can treat 'no partner' correctly
            PartnerId = d.PartnerId,
            CashAccountId = d.CashAccountId,
            PartnerTitle = d.Partner?.Name ?? string.Empty,
            PartnerAddress = d.Partner?.Address ?? string.Empty,
            PartnerTaxId = d.Partner?.TaxNo ?? string.Empty,
            PartnerTaxOffice = string.Empty,
            Status = d.Status.ToString(),
            Currency = d.Currency ?? "TRY",
            // R-249: Map Description and SourceWarehouseId for edit
            Description = d.Description,
            SourceWarehouseId = d.SourceWarehouseId,
            DestinationWarehouseId = d.DestinationWarehouseId,
            Lines = d.Lines.Select(l => new InventoryERP.Application.Documents.DTOs.DocumentLineDto
            {
                Id = l.Id,
                ItemId = l.ItemId,
                ItemName = l.Item != null ? l.Item.Name : string.Empty,  // R-045: Populate ItemName from Item
                Qty = l.Qty,
                UnitPrice = l.UnitPrice,
                Uom = l.Uom,
                Coefficient = l.Coefficient,
                VatRate = l.VatRate,
                LineNet = l.Qty * l.UnitPrice,
                LineVat = l.Qty * l.UnitPrice * l.VatRate / 100m,
                LineGross = l.Qty * l.UnitPrice + (l.Qty * l.UnitPrice * l.VatRate / 100m),
                // R-054: Include location and variant fields so they're preserved when editing
                SourceLocationId = l.SourceLocationId,
                DestinationLocationId = l.DestinationLocationId,
                ProductVariantId = l.ProductVariantId,
                LotId = l.LotId
            }).ToList()
        };

        dto.TotalNet = dto.Lines.Sum(l => l.Qty * l.UnitPrice);
        dto.TotalVat = dto.Lines.Sum(l => l.Qty * l.UnitPrice * l.VatRate / 100m);
        dto.TotalGross = dto.TotalNet + dto.TotalVat;

        return dto;
    }

    private static bool TryParseDocumentType(string? value, out DocumentType type)
        => Enum.TryParse(value, true, out type);

    private static bool TryParseDocumentStatus(string? value, out DocumentStatus status)
        => Enum.TryParse(value, true, out status);
}

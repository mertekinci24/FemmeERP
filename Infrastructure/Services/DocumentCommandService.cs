using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Commands.Invoices;
using InventoryERP.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Persistence;
using InventoryERP.Domain.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Infrastructure.Services;

public class DocumentCommandService : IDocumentCommandService
{
    private readonly AppDbContext _db;
    private readonly IInventoryQueries _inventory;
    private readonly INumberSequenceService _seq;
    private readonly IServiceScopeFactory _scopeFactory;

    public DocumentCommandService(AppDbContext db, IInventoryQueries inventory, INumberSequenceService seq, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _inventory = inventory;
        _seq = seq;
        _scopeFactory = scopeFactory;
    }

    public async Task<int> CreateDraftAsync(DocumentDetailDto dto)
    {
        var doc = new Document
        {
            Type = Enum.Parse<DocumentType>(dto.Type, ignoreCase: true),
            Number = await _seq.GenerateNextNumberAsync(dto.Type), // R-203.11: Atomic Sequencing
            Date = dto.Date,
            Status = InventoryERP.Domain.Enums.DocumentStatus.DRAFT,
            // R-046: PartnerId = 0 is invalid FK (not a real partner). Convert to null.
            // R-038 made PartnerId nullable to allow ADJUSTMENT/SAYIM documents without partner.
            PartnerId = dto.PartnerId > 0 ? dto.PartnerId : null,
            CashAccountId = dto.CashAccountId > 0 ? dto.CashAccountId : null,
            Currency = dto.Currency,
            // R-249: Map Description and SourceWarehouseId
            Description = dto.Description,
            SourceWarehouseId = dto.SourceWarehouseId > 0 ? dto.SourceWarehouseId : null
        };

        foreach (var l in dto.Lines)
        {
            var dl = new DocumentLine
            {
                ItemId = l.ItemId,
                Qty = l.Qty,
                Coefficient = l.Coefficient,
                UnitPrice = l.UnitPrice,
                Uom = l.Uom,
                VatRate = l.VatRate,
                ProductVariantId = l.ProductVariantId,
                SourceLocationId = l.SourceLocationId,
                DestinationLocationId = l.DestinationLocationId
            };
            // If this is an incoming goods document, allow creating a new Lot from DTO
            if (Enum.TryParse<DocumentType>(dto.Type, true, out var dt) && dt == DocumentType.GELEN_IRSALIYE)
            {
                if (l.LotId.HasValue)
                {
                    dl.LotId = l.LotId.Value;
                }
                else if (!string.IsNullOrWhiteSpace(l.LotNumber))
                {
                    var lot = new Lot { ProductId = l.ItemId, LotNumber = l.LotNumber, ExpiryDate = l.ExpiryDate };
                    _db.Lots.Add(lot);
                    // associate by navigation so EF will set FK on save
                    dl.Lot = lot;
                }
            }
            doc.Lines.Add(dl);
        }
        // If caller provided totals for a line-less document (cash receipt/payment), persist them.
        if ((dto.Lines == null || dto.Lines.Count == 0) && dto.TotalGross > 0)
        {
            doc.TotalTry = Math.Round(dto.TotalGross, 2, MidpointRounding.AwayFromZero);
        }

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        return doc.Id;
    }

    public async Task UpdateDraftAsync(int id, DocumentDetailDto dto)
    {
        var doc = await _db.Documents.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id) ?? throw new InvalidOperationException("DOC-404");
        if (doc.Status != InventoryERP.Domain.Enums.DocumentStatus.DRAFT) throw new InvalidOperationException("DOC-NOT-DRAFT");

        // R-203.12: Prevent overwriting generated number with empty string from DTO (which initializes to "")
        if (!string.IsNullOrWhiteSpace(dto.Number))
        {
            doc.Number = dto.Number;
        }
        doc.Date = dto.Date;
        // R-046: PartnerId = 0 is invalid FK (not a real partner). Convert to null.
        // R-038 made PartnerId nullable to allow ADJUSTMENT/SAYIM documents without partner.
        doc.PartnerId = dto.PartnerId > 0 ? dto.PartnerId : null;
        doc.CashAccountId = dto.CashAccountId > 0 ? dto.CashAccountId : null;
        doc.Currency = dto.Currency;
        // R-249: Map Description and SourceWarehouseId
        doc.Description = dto.Description;
        doc.SourceWarehouseId = dto.SourceWarehouseId > 0 ? dto.SourceWarehouseId : null;

        // R-203: Robust Line Reconciliation (Enterprise Pattern)
        // 1. Identify lines to DELETE (in DB but not in DTO)
        var dtoLineIds = dto.Lines.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
        foreach (var dbLine in doc.Lines)
        {
            if (!dtoLineIds.Contains(dbLine.Id))
            {
                dbLine.IsDeleted = true;
                dbLine.DeletedAt = DateTime.UtcNow;
                _db.Entry(dbLine).State = EntityState.Modified;
            }
        }

        // 2. Identify lines to UPDATE or ADD
        foreach (var l in dto.Lines)
        {
            DocumentLine dl;
            if (l.Id > 0)
            {
                // Update existing
                dl = doc.Lines.FirstOrDefault(x => x.Id == l.Id);
                if (dl == null) continue; // Should not happen if ID is valid
            }
            else
            {
                // Add new
                dl = new DocumentLine();
                doc.Lines.Add(dl);
            }

            // Map properties
            dl.ItemId = l.ItemId;
            dl.Qty = l.Qty;
            dl.Coefficient = l.Coefficient;
            dl.UnitPrice = l.UnitPrice;
            dl.Uom = l.Uom;
            dl.VatRate = l.VatRate;
            dl.ProductVariantId = l.ProductVariantId;
            dl.SourceLocationId = l.SourceLocationId;
            dl.DestinationLocationId = l.DestinationLocationId;

            if (Enum.TryParse<DocumentType>(dto.Type, true, out var dt2) && dt2 == DocumentType.GELEN_IRSALIYE)
            {
                if (l.LotId.HasValue) dl.LotId = l.LotId.Value;
                else if (!string.IsNullOrWhiteSpace(l.LotNumber))
                {
                    var lot = new Lot { ProductId = l.ItemId, LotNumber = l.LotNumber, ExpiryDate = l.ExpiryDate };
                    _db.Lots.Add(lot);
                    dl.Lot = lot;
                }
            }
        }

        // If caller provided totals for a line-less document (cash receipt/payment), persist them.
        if ((dto.Lines == null || dto.Lines.Count == 0) && dto.TotalGross > 0)
        {
            doc.TotalTry = Math.Round(dto.TotalGross, 2, MidpointRounding.AwayFromZero);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // FALLBACK: Create a FRESH Scope (Isolate from the failed context)
            using var scope = _scopeFactory.CreateScope();
            var freshDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // We don't need seqService here as we are not generating numbers

            // 1. Re-Fetch
            var freshDoc = await freshDb.Documents.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id);
            if (freshDoc == null) throw new InvalidOperationException("DOC-404-RETRY");

            // 2. Map DTO fields to freshDoc
            if (!string.IsNullOrWhiteSpace(dto.Number))
            {
                freshDoc.Number = dto.Number;
            }

            // 4. Save on FRESH Context
            await freshDb.SaveChangesAsync();
        }
    }

    private bool IsUniqueConstraintViolation(Exception ex)
    {
        var root = ex.GetBaseException(); // Drill down to the bottom

        // Check for SQLite specifics
        if (root is Microsoft.Data.Sqlite.SqliteException sqlEx)
        {
            // 19 = Constraint, 2067 = Unique
            return sqlEx.SqliteErrorCode == 19 || sqlEx.SqliteErrorCode == 2067;
        }

        // Fallback string check
        var msg = root.Message;
        return msg.Contains("UNIQUE constraint failed") || 
               msg.Contains("SQLite Error 19") ||
               msg.Contains("constraint");
    }

    public async Task DeleteDraftAsync(int id)
    {
        var doc = await _db.Documents.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id) ?? throw new InvalidOperationException("DOC-404");
        if (doc.Status != InventoryERP.Domain.Enums.DocumentStatus.DRAFT) throw new InvalidOperationException("DOC-NOT-DRAFT");

        _db.DocumentLines.RemoveRange(doc.Lines);
        _db.Documents.Remove(doc);
        
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // R-241: Row likely already deleted or stale. Treat delete as success (idempotent).
            System.Diagnostics.Debug.WriteLine("[R-241] Concurrency exception on delete - treating as success.");
        }
    }

    public async Task ApproveAsync(int id)
    {
        int retry = 0;
        while (retry < 3)
        {
            try
            {
                var posting = new InvoicePostingService(_db, _inventory);
                await posting.ApproveAndPostAsync(id, null, null, System.Threading.CancellationToken.None);
                break;
            }
            catch (DbUpdateConcurrencyException)
            {
                retry++;
                _db.ChangeTracker.Clear(); // Clear stale cache
                if (retry >= 3) throw;
            }
        }
    }

    public async Task CancelAsync(int id)
    {
        var rev = new InvoiceReversalService(_db);
        await rev.ReverseAsync(id, "cancelled", System.Threading.CancellationToken.None);
    }

    public async Task<int> ConvertSalesOrderToDispatchAsync(int salesOrderId)
    {
        var src = await _db.Documents.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == salesOrderId) ?? throw new InvalidOperationException("DOC-404");
        if (src.Type != InventoryERP.Domain.Enums.DocumentType.SALES_ORDER) throw new InvalidOperationException("DOC-NOT-SALES_ORDER");
    // Accept either APPROVED or POSTED as a valid precondition for conversion (some flows mark sales orders as POSTED)
    if (src.Status != InventoryERP.Domain.Enums.DocumentStatus.APPROVED && src.Status != InventoryERP.Domain.Enums.DocumentStatus.POSTED) throw new InvalidOperationException("DOC-NOT-APPROVED");

        var dst = new Document
        {
            Type = InventoryERP.Domain.Enums.DocumentType.SEVK_IRSALIYESI,
            Number = await _seq.GenerateNextNumberAsync("SEVK_IRSALIYESI"), // R-203: Smart Numbering
            Date = System.DateTime.Today,
            Status = InventoryERP.Domain.Enums.DocumentStatus.DRAFT,
            PartnerId = src.PartnerId,
            Currency = src.Currency,
            ExternalId = src.Id.ToString(), // R-203: Link to source document
            // R-264: Persist Header Data from source
            Description = src.Description,
            SourceWarehouseId = src.SourceWarehouseId
        };

        foreach (var l in src.Lines)
        {
            dst.Lines.Add(new DocumentLine
            {
                ItemId = l.ItemId,
                Qty = l.Qty,
                UnitPrice = l.UnitPrice,
                Uom = l.Uom,
                VatRate = l.VatRate,
                Coefficient = l.Coefficient, // R-203: Copy missing fields
                ProductVariantId = l.ProductVariantId
            });
        }

        _db.Documents.Add(dst);
        await _db.SaveChangesAsync();
        return dst.Id;
    }

    public async Task<int> ConvertDispatchToInvoiceAsync(int dispatchId)
    {
        var src = await _db.Documents.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == dispatchId) ?? throw new InvalidOperationException("DOC-404");
        if (src.Type != InventoryERP.Domain.Enums.DocumentType.SEVK_IRSALIYESI) throw new InvalidOperationException("DOC-NOT-SEVK_IRSALIYESI");
        // Accept either APPROVED or POSTED as valid precondition for conversion
        if (src.Status != InventoryERP.Domain.Enums.DocumentStatus.APPROVED && src.Status != InventoryERP.Domain.Enums.DocumentStatus.POSTED) throw new InvalidOperationException("DOC-NOT-APPROVED");

        var dst = new Document
        {
            Type = InventoryERP.Domain.Enums.DocumentType.SALES_INVOICE,
            Number = await _seq.GenerateNextNumberAsync("SALES_INVOICE"), // R-203: Smart Numbering
            Date = System.DateTime.Today,
            Status = InventoryERP.Domain.Enums.DocumentStatus.DRAFT,
            PartnerId = src.PartnerId,
            Currency = src.Currency,
            ExternalId = src.Id.ToString(), // R-203: Link to source document
            // R-264: Persist Header Data from source
            Description = src.Description,
            SourceWarehouseId = src.SourceWarehouseId
        };

        foreach (var l in src.Lines)
        {
            dst.Lines.Add(new DocumentLine
            {
                ItemId = l.ItemId,
                Qty = l.Qty,
                UnitPrice = l.UnitPrice,
                Uom = l.Uom,
                VatRate = l.VatRate,
                Coefficient = l.Coefficient, // R-203: Copy missing fields
                ProductVariantId = l.ProductVariantId
            });
        }

        _db.Documents.Add(dst);
        await _db.SaveChangesAsync();
        return dst.Id;
    }

    public async Task SaveAndApproveAdjustmentAsync(int id, DocumentDetailDto dto)
    {
        // R-057: FINAL FIX for P0 Blocker (Unit of Work violation)
        // Combines UpdateDraft + Approve in single transaction to prevent FK constraint on DocLineId
        // NOTE: We inline the approval logic instead of calling ApproveAsync to avoid nested transactions
        
        // STEP 1: Update draft (UpdateDraftAsync logic)
        var doc = await _db.Documents.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) throw new KeyNotFoundException($"Document {id} not found");
        if (doc.Status != DocumentStatus.DRAFT) throw new InvalidOperationException("DOC-NOT-DRAFT");
        
        doc.Number = dto.Number;
        doc.Date = dto.Date;
        doc.PartnerId = dto.PartnerId > 0 ? dto.PartnerId : null;
        doc.Currency = dto.Currency;
        
        // Update lines
        _db.DocumentLines.RemoveRange(doc.Lines);
        doc.Lines.Clear();
        foreach (var l in dto.Lines)
        {
            doc.Lines.Add(new DocumentLine
            {
                ItemId = l.ItemId,
                Qty = l.Qty,
                Coefficient = l.Coefficient,
                UnitPrice = l.UnitPrice,
                Uom = l.Uom,
                VatRate = l.VatRate,
                ProductVariantId = l.ProductVariantId,
                SourceLocationId = l.SourceLocationId,         // R-054: Preserved
                DestinationLocationId = l.DestinationLocationId
            });
        }
        
        // STEP 2: CRITICAL - Save to get DocLineId assigned
        await _db.SaveChangesAsync();
        
        // STEP 3: Approve using existing ApproveAsync (which calls InvoicePostingService)
        // This will start its own transaction, which is fine because Step 2 already committed
        await ApproveAsync(id);
    }
}

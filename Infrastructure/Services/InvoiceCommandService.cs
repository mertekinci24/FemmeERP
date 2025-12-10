using System;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using Microsoft.EntityFrameworkCore;
using Persistence;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Infrastructure.Services;

public sealed class InvoiceCommandService : IInvoiceCommandService
{
    private readonly AppDbContext _db;
    private readonly INumberSequenceService _seq;
    private readonly IInvoicePostingService _postingService;

    public InvoiceCommandService(AppDbContext db, INumberSequenceService seq, IInvoicePostingService postingService)
    {
        _db = db;
        _seq = seq;
        _postingService = postingService;
    }

    public async Task<int> CreateDraftAsync(CreateInvoiceDraftDto cmd)
    {
        var docType = Enum.Parse<DocumentType>(cmd.Type, ignoreCase: true);
        var doc = new Document
        {
            Type = docType,
            Number = await _seq.GenerateNextNumberAsync(cmd.Type), // R-203.11: Atomic Sequencing
            Date = cmd.Date,
            Status = DocumentStatus.DRAFT
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        return doc.Id;
    }

    public async Task ApproveAsync(ApproveInvoiceDto cmd)
    {
        // R-206.1: Delegate to Posting Engine to ensure Stock Moves and Ledger Entries are created.
        // Previously, this method only updated the status, causing "Fake Approval".
        await _postingService.ApproveAsync(
            new InventoryERP.Infrastructure.Commands.Invoices.ApproveInvoiceCommand(cmd.DocumentId, null, null), 
            System.Threading.CancellationToken.None);
    }

    public async Task CancelAsync(CancelInvoiceDto cmd)
    {
        var doc = await _db.Documents.SingleAsync(d => d.Id == cmd.DocumentId);
        doc.Status = DocumentStatus.CANCELED;
        await _db.SaveChangesAsync();
    }
}

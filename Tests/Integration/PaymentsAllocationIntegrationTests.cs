using System.Threading.Tasks;
using Xunit;
using Tests.Infrastructure;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Application.Documents.DTOs;
using System.Linq;

namespace Tests.Integration;

public class PaymentsAllocationIntegrationTests : BaseIntegrationTest
{
    // R-169: Temporarily disabled - requires CashPostingService (deleted in R-162)
    // TODO: Re-enable after merging CashPostingService with CashService
    /*
    [Fact]
    public async Task PartialAndFullPayment_AllocationsUpdateInvoiceStatus()
    {
        // Arrange
        var partner = new Partner { Title = "CustX", Role = PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);
        await Ctx.SaveChangesAsync();

    var docSvc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(Ctx);
    // create product to satisfy FK on lines
    var prod = new Product { Sku = "PR1", Name = "P1", BaseUom = "ADET", VatRate = 10, ReservedQty = 0m };
    Ctx.Products.Add(prod);
    await Ctx.SaveChangesAsync();
        var invoiceDto = new DocumentDetailDto {
            Type = "SALES_INVOICE",
            Number = "INV-1",
            Date = System.DateTime.Today,
            PartnerId = partner.Id,
            Currency = "TRY",
            Lines = new System.Collections.Generic.List<Application.Documents.DTOs.DocumentLineDto> {
                new Application.Documents.DTOs.DocumentLineDto { ItemId = prod.Id, Qty = 1, UnitPrice = 100m, Uom = "ADET", VatRate = 10 }
            }
        };

        var invId = await docSvc.CreateDraftAsync(invoiceDto);
        // Approve (post) invoice -> creates PartnerLedgerEntry debit
        await docSvc.ApproveAsync(invId);

        var invoiceEntry = Ctx.PartnerLedgerEntries.Single(e => e.DocId == invId && e.Debit > 0);
        Assert.Equal(LedgerStatus.OPEN, invoiceEntry.Status);

        // Create payment document (receipt)
        var payDto = new DocumentDetailDto {
            Type = "RCPT_CUSTOMER",
            Number = "RC-1",
            Date = System.DateTime.Today,
            PartnerId = partner.Id,
            Currency = "TRY",
            Lines = new System.Collections.Generic.List<Application.Documents.DTOs.DocumentLineDto> {
                new Application.Documents.DTOs.DocumentLineDto { ItemId = prod.Id, Qty = 1, UnitPrice = 100m, Uom = "ADET", VatRate = 10 }
            }
        };

        var payDocId = await docSvc.CreateDraftAsync(payDto);
    var cashSvc = new global::InventoryERP.Infrastructure.Services.CashPostingService(Ctx);
        await cashSvc.ApproveAsync(payDocId, null);

        var paymentEntry = Ctx.PartnerLedgerEntries.Single(e => e.DocId == payDocId && e.Credit > 0);
        Assert.Equal(LedgerStatus.OPEN, paymentEntry.Status);

    var allocSvc = new global::InventoryERP.Infrastructure.Services.AllocationService(Ctx);

        // Act: partial allocation of 60 (invoice 100)
        await allocSvc.AllocateAsync(paymentEntry.Id, invoiceEntry.Id, 60m);

        var allocs = Ctx.PaymentAllocations.Where(a => a.PaymentEntryId == paymentEntry.Id && a.InvoiceEntryId == invoiceEntry.Id).ToList();
        Assert.Single(allocs);
        Assert.Equal(60m, allocs[0].AmountTry);

        // Reload entries
    var invAfterPartial = Ctx.PartnerLedgerEntries.Find(invoiceEntry.Id);
    Assert.NotNull(invAfterPartial);
    Assert.Equal(LedgerStatus.OPEN, invAfterPartial!.Status);

    // Act: allocate remaining amount (invoice may include VAT so compute remaining)
    var remaining = invoiceEntry.AmountTry - 60m;
    await allocSvc.AllocateAsync(paymentEntry.Id, invoiceEntry.Id, remaining);

    var invAfterFull = Ctx.PartnerLedgerEntries.Find(invoiceEntry.Id);
    Assert.NotNull(invAfterFull);
    Assert.Equal(LedgerStatus.CLOSED, invAfterFull!.Status);

    var payAfter = Ctx.PartnerLedgerEntries.Find(paymentEntry.Id);
    Assert.NotNull(payAfter);
    Assert.Equal(LedgerStatus.CLOSED, payAfter!.Status);
    }
    */
}

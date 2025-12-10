using System.Threading.Tasks;
using Xunit;
using Tests.Infrastructure;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using System;

namespace Tests.Integration;

public class DocumentsCommandTests : BaseIntegrationTest
{
    [Fact]
    public async Task CreateApproveCancel_Idempotency_Works()
    {
        // Arrange
        var partner = new Partner { Title = "P1", Role = PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);
        await Ctx.SaveChangesAsync();

        var svc = new InvoiceCommandService(Ctx);

        // Act - create draft
        var id = await svc.CreateDraftAsync(new Application.Documents.CreateInvoiceDraftDto("SALES_INVOICE", DateTime.Today));

        // Approve twice (idempotent)
        await svc.ApproveAsync(new Application.Documents.ApproveInvoiceDto(id));
        await svc.ApproveAsync(new Application.Documents.ApproveInvoiceDto(id));

        var doc = await Ctx.Documents.FindAsync(id);
        Assert.NotNull(doc);
        Assert.Equal(DocumentStatus.APPROVED, doc.Status);

        // Cancel twice (idempotent)
        await svc.CancelAsync(new Application.Documents.CancelInvoiceDto(id));
        await svc.CancelAsync(new Application.Documents.CancelInvoiceDto(id));

        doc = await Ctx.Documents.FindAsync(id);
        Assert.NotNull(doc);
        Assert.Equal(DocumentStatus.CANCELED, doc.Status);
    }
}

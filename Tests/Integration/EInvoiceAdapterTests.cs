using System.Threading.Tasks;
using InventoryERP.Application.EInvoicing;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using FluentAssertions;
using InventoryERP.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Integration;

public class EInvoiceAdapterTests : BaseIntegrationTest
{
    [Fact]
    public async Task SendInvoice_Sets_Status_To_SENT()
    {
        // Arrange
    var doc = new Document { Type = DocumentType.SALES_INVOICE, Number = "INV-001", Date = System.DateTime.Today, Status = DocumentStatus.APPROVED };
        Ctx.Documents.Add(doc);
        Ctx.SaveChanges();

        var services = new ServiceCollection();
        services.AddScoped(_ => Ctx);
        services.AddInfrastructure();
        using var sp = services.BuildServiceProvider();

        var adapter = sp.GetRequiredService<IEInvoiceAdapter>();

        // Act
        await adapter.SendInvoiceAsync(doc.Id);

        // Assert
        var refreshed = Ctx.Documents.Find(doc.Id)!;
        refreshed.Status.Should().Be(DocumentStatus.SENT);
    }
}

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using InventoryERP.Domain.Entities;
using InventoryERP.Infrastructure.Queries;
using Tests.Infrastructure;

namespace Tests.Integration.Reports;

public class GetTopProductsTests : BaseIntegrationTest
{
    [Fact]
    public async Task DashboardQueries_Returns_TopProducts_By_Revenue()
    {
        // arrange
    var prod1 = new Product { Sku = "SKU1", Name = "Prod A", BaseUom = "pcs", VatRate = 20 };
    var prod2 = new Product { Sku = "SKU2", Name = "Prod B", BaseUom = "pcs", VatRate = 20 };
        Ctx.Products.Add(prod1);
        Ctx.Products.Add(prod2);
        await Ctx.SaveChangesAsync();

        var doc1 = new Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Status = Domain.Enums.DocumentStatus.POSTED, Date = new DateTime(2025,10,1), FxRate = 1m };
        var doc2 = new Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Status = Domain.Enums.DocumentStatus.POSTED, Date = new DateTime(2025,10,2), FxRate = 1m };
        Ctx.Documents.Add(doc1);
        Ctx.Documents.Add(doc2);
        await Ctx.SaveChangesAsync();

        // doc1: prod1 qty 2 @ 100 => revenue 200
    Ctx.DocumentLines.Add(new DocumentLine { DocumentId = doc1.Id, ItemId = prod1.Id, Qty = 2m, UnitPrice = 100m, Uom = "pcs", VatRate = 20 });
        // doc2: prod2 qty 5 @ 60 => revenue 300
    Ctx.DocumentLines.Add(new DocumentLine { DocumentId = doc2.Id, ItemId = prod2.Id, Qty = 5m, UnitPrice = 60m, Uom = "pcs", VatRate = 20 });
        // doc2: prod1 qty 1 @ 100 => revenue +100 (prod1 total 300)
    Ctx.DocumentLines.Add(new DocumentLine { DocumentId = doc2.Id, ItemId = prod1.Id, Qty = 1m, UnitPrice = 100m, Uom = "pcs", VatRate = 20 });

        await Ctx.SaveChangesAsync();

        var svc = new DashboardQueriesEf(Ctx);

        // act
        var res = await svc.GetTopProductsAsync(new DateTime(2025,9,30), new DateTime(2025,10,31), topN: 5);

        // assert
        res.Should().NotBeNull();
    res.TopProducts.Count.Should().BeGreaterThanOrEqualTo(2);
        // prod1 revenue = 300, prod2 revenue = 300 => tie, but prod2 has higher quantity (5) vs prod1 (3), ordering by revenue then qty desc
        var first = res.TopProducts[0];
        first.Sku.Should().Be("SKU2");
        first.RevenueTry.Should().Be(300m);
        var second = res.TopProducts[1];
        second.Sku.Should().Be("SKU1");
        second.RevenueTry.Should().Be(300m);
    }
}

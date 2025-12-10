using InventoryERP.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;

namespace Tests.Integration;

public class ProductUniqueSkuTests : BaseIntegrationTest
{
    [Fact]
    public void DuplicateSku_Should_Fail()
    {
        Ctx.Products.Add(new Product { Sku = "SKU-1", Name = "A", BaseUom = "EA", VatRate = 20 });
        Ctx.SaveChanges();

        Ctx.Products.Add(new Product { Sku = "SKU-1", Name = "B", BaseUom = "EA", VatRate = 20 });

        Action act = () => Ctx.SaveChanges();
        act.Should().Throw<DbUpdateException>();
    }
}


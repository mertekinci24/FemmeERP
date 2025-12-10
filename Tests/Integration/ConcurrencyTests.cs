using InventoryERP.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;

namespace Tests.Integration;

public class ConcurrencyTests : BaseIntegrationTest
{
    [Fact]
    public void ModifiedAt_Version_ConcurrencyToken_Should_Detect_Conflict()
    {
        var prod = new Product { Sku = Guid.NewGuid().ToString(), Name = "C", BaseUom = "EA", VatRate = 20 };
        Ctx.Products.Add(prod);
        Ctx.SaveChanges();

        var opt = new DbContextOptionsBuilder<Persistence.AppDbContext>().UseSqlite(Conn).Options;
        using var ctx2 = new Persistence.AppDbContext(opt);

        var p1 = Ctx.Products.First(p => p.Id == prod.Id);
        var p2 = ctx2.Products.First(p => p.Id == prod.Id);

        p1.Name = "C1"; Ctx.SaveChanges();

        p2.Name = "C2";
        Action act = () => ctx2.SaveChanges();
        act.Should().Throw<DbUpdateConcurrencyException>();
    }
}


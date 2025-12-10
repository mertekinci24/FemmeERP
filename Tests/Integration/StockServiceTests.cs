using System.Linq;
using InventoryERP.Infrastructure.Services;
using Persistence;
using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;

using Tests.Infrastructure;
namespace Tests.Integration;

public class StockServiceTests : BaseIntegrationTest
{
    [Fact]
    public async Task PostMoveAsync_Should_Throw_On_Negative_Stock()
    {
        var p = new Product { Sku = "A1", Name = "Test", BaseUom = "EA", VatRate = 20 };
        Ctx.Products.Add(p); await Ctx.SaveChangesAsync();
        var queries = new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx);
        var service = new StockService(Ctx, queries);
        await service.PostMoveAsync(p.Id, 10, true);
        Func<Task> act = async () => await service.PostMoveAsync(p.Id, 20, false);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*STK-NEG-001*");
    }

    [Fact]
    public async Task PostMoveAsync_Should_Add_StockMove()
    {
        var p = new Product { Sku = "A2", Name = "Test2", BaseUom = "EA", VatRate = 10 };
        Ctx.Products.Add(p); await Ctx.SaveChangesAsync();
        var queries = new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx);
        var service = new StockService(Ctx, queries);
        await service.PostMoveAsync(p.Id, 5, true);
        var onHand = (await Ctx.StockMoves
            .Where(x => x.ItemId == p.Id)
            .Select(x => x.QtySigned)
            .ToListAsync())
            .Sum();
        onHand.Should().Be(5);
    }
}

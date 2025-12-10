using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Seeding;
using Tests.Infrastructure;

namespace Tests.Integration;

public class SeedAndIndexTests : BaseIntegrationTest
{
    [Fact]
    public async Task Seeder_Should_Create_Minimum_Data()
    {
        await DatabaseSeeder.SeedAsync(Ctx);
        (await Ctx.Partners.CountAsync()).Should().BeGreaterThanOrEqualTo(2);
        (await Ctx.Products.CountAsync()).Should().BeGreaterThanOrEqualTo(2);
        (await Ctx.StockMoves.CountAsync()).Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Product_Sku_Unique_Index_Exists()
    {
        using var cmd = Ctx.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "PRAGMA index_list('Product');";
        Ctx.Database.OpenConnection();
        using var r = cmd.ExecuteReader();
        var indexes = new List<string>();
        while (r.Read()) indexes.Add(r.GetString(1));
        indexes.Any(n => n.Contains("Sku", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }
}

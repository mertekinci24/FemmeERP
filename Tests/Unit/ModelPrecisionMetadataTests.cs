using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Tests.Infrastructure;

namespace Tests.Unit;

public class ModelPrecisionMetadataTests : BaseIntegrationTest
{
    [Fact]
    public void Precision_Metadata_Is_Configured()
    {
        var model = Ctx.Model;
        var ple = model.FindEntityType(typeof(Persistence.AppDbContext))?.ClrType; // guard
        var pleType = model.FindEntityType(typeof(Domain.Entities.PartnerLedgerEntry))!;
        pleType.FindProperty("Debit")!.GetPrecision().Should().Be(18);
        pleType.FindProperty("Debit")!.GetScale().Should().Be(2);
        pleType.FindProperty("Credit")!.GetPrecision().Should().Be(18);
        pleType.FindProperty("Credit")!.GetScale().Should().Be(2);
        pleType.FindProperty("AmountTry")!.GetPrecision().Should().Be(18);
        pleType.FindProperty("AmountTry")!.GetScale().Should().Be(2);

        var smType = model.FindEntityType(typeof(Domain.Entities.StockMove))!;
        smType.FindProperty("QtySigned")!.GetPrecision().Should().Be(18);
        smType.FindProperty("QtySigned")!.GetScale().Should().Be(3);
        smType.FindProperty("UnitCost")!.GetPrecision().Should().Be(18);
        smType.FindProperty("UnitCost")!.GetScale().Should().Be(6);
    }
}


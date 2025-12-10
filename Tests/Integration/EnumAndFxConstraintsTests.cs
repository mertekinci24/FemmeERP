using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;

namespace Tests.Integration;

public class EnumAndFxConstraintsTests : BaseIntegrationTest
{
    [Fact]
    public void Enum_Roundtrip_Document()
    {
        var d = new Document
        {
            Type = DocumentType.SALES_INVOICE,
            Number = "E1",
            Date = DateTime.UtcNow,
            Status = DocumentStatus.POSTED,
            Currency = "USD",
            FxRate = 1.5m
        };
        Ctx.Documents.Add(d);
        Ctx.SaveChanges();
        Ctx.Entry(d).Reload();
        d.Type.Should().Be(DocumentType.SALES_INVOICE);
        d.Status.Should().Be(DocumentStatus.POSTED);
    }

    [Fact]
    public void ExternalId_PartialUnique_AllowsNulls_BlocksDuplicateNonNull()
    {
        Ctx.Documents.Add(new Document { Type = DocumentType.SALES_INVOICE, Number = "N1", Date = DateTime.UtcNow, Status = DocumentStatus.POSTED, Currency = "TRY", FxRate = 1m, ExternalId = null });
        Ctx.Documents.Add(new Document { Type = DocumentType.SALES_INVOICE, Number = "N2", Date = DateTime.UtcNow, Status = DocumentStatus.POSTED, Currency = "TRY", FxRate = 1m, ExternalId = null });
        Ctx.SaveChanges();

        Ctx.Documents.Add(new Document { Type = DocumentType.SALES_INVOICE, Number = "N3", Date = DateTime.UtcNow, Status = DocumentStatus.POSTED, Currency = "TRY", FxRate = 1m, ExternalId = "X-1" });
        Ctx.SaveChanges();
        Ctx.Documents.Add(new Document { Type = DocumentType.SALES_INVOICE, Number = "N4", Date = DateTime.UtcNow, Status = DocumentStatus.POSTED, Currency = "TRY", FxRate = 1m, ExternalId = "X-1" });
        Action act = () => Ctx.SaveChanges();
        act.Should().Throw<DbUpdateException>();
    }

    [Fact]
    public void FxRate_Zero_For_NonTry_Should_Fail()
    {
        var d = new Document
        {
            Type = DocumentType.SALES_INVOICE,
            Number = "E2",
            Date = DateTime.UtcNow,
            Status = DocumentStatus.POSTED,
            Currency = "USD",
            FxRate = 0m
        };
        Ctx.Documents.Add(d);
        Action act = () => Ctx.SaveChanges();
        act.Should().Throw<DbUpdateException>();
    }

    [Fact]
    public void FxRate_Zero_For_Try_Should_Pass()
    {
        var d = new Document
        {
            Type = DocumentType.SALES_INVOICE,
            Number = "E3",
            Date = DateTime.UtcNow,
            Status = DocumentStatus.POSTED,
            Currency = "TRY",
            FxRate = 0m
        };
        Ctx.Documents.Add(d);
        Ctx.SaveChanges();
    }
}

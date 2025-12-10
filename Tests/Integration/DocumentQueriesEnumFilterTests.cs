using System;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Queries;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// TST-022: Guards against LINQ translation errors caused by enum ToString() usage in DocumentQueries.
/// </summary>
public class DocumentQueriesEnumFilterTests : BaseIntegrationTest
{
    [Fact]
    public async Task ListAsync_Filters_By_Type_Without_Linq_Translation_Errors()
    {
        // Arrange
        var db = Ctx;
        var partner = new Partner { Title = "Quote Customer", Role = PartnerRole.CUSTOMER };
        db.Partners.Add(partner);
        await db.SaveChangesAsync();

        db.Documents.Add(new Document
        {
            Type = DocumentType.QUOTE,
            Status = DocumentStatus.DRAFT,
            Date = new DateTime(2025, 11, 14),
            Number = "Q-0001",
            PartnerId = partner.Id,
            TotalTry = 1000m
        });

        db.Documents.Add(new Document
        {
            Type = DocumentType.SALES_INVOICE,
            Status = DocumentStatus.POSTED,
            Date = new DateTime(2025, 11, 10),
            Number = "INV-0001",
            PartnerId = partner.Id,
            TotalTry = 500m
        });

        await db.SaveChangesAsync();

        var queries = new DocumentQueries(db);
        var filter = new DocumentListFilter
        {
            Type = DocumentType.QUOTE.ToString(),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await queries.ListAsync(filter, filter.Page, filter.PageSize);

        // Assert – previously threw "could not be translated" due to d.Type.ToString()
        Assert.Equal(1, result.TotalCount);
        var quoteRow = Assert.Single(result.Items);
        Assert.Equal("Q-0001", quoteRow.Number);
        Assert.Equal(DocumentType.QUOTE.ToString(), quoteRow.Type);
    }
}

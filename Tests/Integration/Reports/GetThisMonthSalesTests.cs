using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using InventoryERP.Domain.Entities;
using InventoryERP.Infrastructure.CQRS.Handlers;
using InventoryERP.Infrastructure.CQRS.Queries;
using Tests.Infrastructure;

namespace Tests.Integration.Reports;

public class GetThisMonthSalesTests : BaseIntegrationTest
{
    [Fact]
    public async Task Handler_Returns_Total_And_Count_For_Specified_Month()
    {
        // arrange - create two posted sales invoices in March 2025
        var d1 = new Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Status = Domain.Enums.DocumentStatus.POSTED, Date = new DateTime(2025,3,5), TotalTry = 100m };
        var d2 = new Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Status = Domain.Enums.DocumentStatus.POSTED, Date = new DateTime(2025,3,20), TotalTry = 200m };
        // other document outside month
        var d3 = new Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Status = Domain.Enums.DocumentStatus.POSTED, Date = new DateTime(2025,4,1), TotalTry = 50m };

        Ctx.Documents.Add(d1);
        Ctx.Documents.Add(d2);
        Ctx.Documents.Add(d3);
        await Ctx.SaveChangesAsync();

        var handler = new GetThisMonthSalesHandler(Ctx);

        // act
        var dto = await handler.Handle(new GetThisMonthSalesQuery(2025,3), default);

        // assert
        dto.Should().NotBeNull();
        dto.Year.Should().Be(2025);
        dto.Month.Should().Be(3);
        dto.InvoiceCount.Should().Be(2);
        dto.TotalTry.Should().Be(300m);
    }
}

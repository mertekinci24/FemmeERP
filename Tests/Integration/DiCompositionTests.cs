using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Persistence;
using Microsoft.Data.Sqlite;
using Tests.Infrastructure;
using InventoryERP.Infrastructure;
using InventoryERP.Application.Stocks;
using InventoryERP.Application.Partners;

namespace Tests.Integration;

public class DiCompositionTests : BaseIntegrationTest
{
    [Fact]
    public void ServiceProvider_Resolves_Infrastructure_Services()
    {
        // Use the existing in-memory context instance from BaseIntegrationTest
        var services = new ServiceCollection();
        services.AddScoped(_ => Ctx as AppDbContext);
        services.AddInfrastructure();

        using var sp = services.BuildServiceProvider();

        sp.GetService<IStockQueries>().Should().NotBeNull();
        sp.GetService<IStockExportService>().Should().NotBeNull();
        sp.GetService<IPartnerReadService>().Should().NotBeNull();
        sp.GetService<IPartnerExportService>().Should().NotBeNull();
    }
}

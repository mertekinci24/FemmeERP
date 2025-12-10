using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
// avoid ambiguity with Tests.Infrastructure namespace; fully-qualify service types when used
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;
using Moq;
using InventoryERP.Domain.Interfaces;
using Xunit;

namespace Tests.Integration;

public class InventoryValuationTests
{
    [Fact]
    public async Task Valuation_AsOfDate_Returns_Total_150_TRY()
    {
        var (ctx, conn) = TestDbContextFactory.Create();
        try
        {
            // Setup: partner and product
            var partner = new Partner { Title = "Tedarikçi", Role = PartnerRole.SUPPLIER };
            ctx.Partners.Add(partner);
            await ctx.SaveChangesAsync();

            var prod = new Product { Sku = "A-001", Name = "Ürün A", BaseUom = "ADET", VatRate = 1, Active = true };
            ctx.Products.Add(prod);
            await ctx.SaveChangesAsync();

            // Goods receipt: 10 units x 10 TRY
            var cmd = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(ctx));
            var grDto = new DocumentDetailDto
            {
                Type = "GELEN_IRSALIYE",
                Number = "GR-VAL-1",
                Date = DateTime.Today,
                Currency = "TRY",
                PartnerId = partner.Id,
                Lines = new List<DocumentLineDto>
                {
                    new DocumentLineDto { ItemId = prod.Id, ItemName = prod.Name, Qty = 10m, Uom = "ADET", Coefficient = 1m, UnitPrice = 10m, VatRate = 1 }
                }
            };
            var grId = await cmd.CreateDraftAsync(grDto);
            await cmd.ApproveAsync(grId);

            // Purchase invoice: 50 TRY additional cost
            var invDto = new DocumentDetailDto { Type = "PURCHASE_INVOICE", Number = "INV-FRT-VAL", Date = DateTime.Today, Currency = "TRY", PartnerId = partner.Id, Lines = new List<DocumentLineDto>(), TotalGross = 50m };
            var invId = await cmd.CreateDraftAsync(invDto);
            await cmd.ApproveAsync(invId);

            // Apply landed cost
            var lc = new global::InventoryERP.Infrastructure.Services.LandedCostService(ctx);
            await lc.ApplyAsync(invId, new[] { grId });

            // Valuation as of today
            var valuation = new global::InventoryERP.Infrastructure.Services.InventoryValuationService(ctx);
            var total = await valuation.GetTotalInventoryValueAsync(DateTime.Today);

            Assert.Equal(150m, total);
        }
        finally
        {
            conn.Dispose();
        }
    }
}

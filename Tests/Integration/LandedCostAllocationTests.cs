using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using Moq;
using InventoryERP.Domain.Interfaces;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Integration;

public class LandedCostAllocationTests
{
    [Fact]
    public async Task Landed_Cost_Distribution_By_Quantity_Updates_Move_And_MWA()
    {
        // Arrange: in-memory DB with migrations
        var (ctx, conn) = TestDbContextFactory.Create();
        try
        {
            // Create partner and product A
            var partner = new Partner { Title = "Tedarikçi", Role = PartnerRole.SUPPLIER };
            ctx.Partners.Add(partner);
            await ctx.SaveChangesAsync();

            var prod = new Product { Sku = "A-001", Name = "Ürün A", BaseUom = "ADET", VatRate = 1, Active = true };
            ctx.Products.Add(prod);
            await ctx.SaveChangesAsync();

            // Step 1: Create a GELEN_IRSALIYE with 10 units at 10 TRY
            var cmd = new DocumentCommandService(ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(ctx));
            var grDto = new DocumentDetailDto
            {
                Type = "GELEN_IRSALIYE",
                Number = "GR-1",
                Date = DateTime.Today,
                Currency = "TRY",
                PartnerId = partner.Id,
                Lines = new List<DocumentLineDto>
                {
                    // Use a valid VAT rate per DB check constraint (1,10,20). Match product's VAT for consistency.
                    new DocumentLineDto { ItemId = prod.Id, ItemName = prod.Name, Qty = 10m, Uom = "ADET", Coefficient = 1m, UnitPrice = 10m, VatRate = 1 }
                }
            };
            var grId = await cmd.CreateDraftAsync(grDto);
            await cmd.ApproveAsync(grId);

            // Verify Product.Cost is 10 and StockMove UnitCost is 10
            var prodReload = await ctx.Products.FirstAsync(p => p.Id == prod.Id);
            Assert.Equal(10m, prodReload.Cost);
            var move = await ctx.StockMoves.Include(m => m.DocumentLine).FirstAsync(m => m.ItemId == prod.Id && m.QtySigned > 0);
            Assert.Equal(10m, move.UnitCost);

            // Step 2: Create a PURCHASE_INVOICE for 50 TRY (navlun)
            var invDto = new DocumentDetailDto { Type = "PURCHASE_INVOICE", Number = "INV-FRT-1", Date = DateTime.Today, Currency = "TRY", PartnerId = partner.Id, Lines = new List<DocumentLineDto>() , TotalGross = 50m };
            var invId = await cmd.CreateDraftAsync(invDto);
            await cmd.ApproveAsync(invId);

            // Step 3: Apply landed cost by quantity to the goods receipt
            var svc = new LandedCostService(ctx);
            await svc.ApplyAsync(invId, new []{ grId });

            // Step 4: The original StockMove unit cost is now 15 (10 + 50/10)
            var moveAfter = await ctx.StockMoves.FirstAsync(m => m.Id == move.Id);
            Assert.Equal(15m, Math.Round(moveAfter.UnitCost ?? 0m, 2));

            // Step 5: Product.MWA (Cost) updated to 15
            var prodAfter = await ctx.Products.FirstAsync(p => p.Id == prod.Id);
            Assert.Equal(15m, Math.Round(prodAfter.Cost, 2));
        }
        finally
        {
            conn.Dispose();
        }
    }
}

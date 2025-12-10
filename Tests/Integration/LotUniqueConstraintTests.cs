using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using InventoryERP.Domain.Entities;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using InventoryERP.Domain.Entities;
using Xunit;

namespace Tests.Integration
{
    public class LotUniqueConstraintTests : Tests.Infrastructure.BaseIntegrationTest
    {
        [Fact]
        public async Task Adding_Duplicate_Lot_For_Same_Product_Throws_DbUpdateException()
        {
            // Arrange
            var ctx = Ctx; // use the context provided by BaseIntegrationTest

            var product = new Product { Sku = "UNIQ1", Name = "UniqueLotProduct", BaseUom = "ADET", VatRate = 10, ReservedQty = 0m };
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();

            var lot1 = new Lot { ProductId = product.Id, LotNumber = "L-XYZ", ExpiryDate = null };
            ctx.Lots.Add(lot1);
            await ctx.SaveChangesAsync();

            // Act - add duplicate
            var lot2 = new Lot { ProductId = product.Id, LotNumber = "L-XYZ", ExpiryDate = null };
            ctx.Lots.Add(lot2);

            // Assert
            await Assert.ThrowsAsync<DbUpdateException>(async () => await ctx.SaveChangesAsync());
        }
    }
}

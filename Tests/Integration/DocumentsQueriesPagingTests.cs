using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Infrastructure.Queries;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Integration
{
    public class DocumentsQueriesPagingTests : BaseIntegrationTest
    {
        [Fact]
        public async Task ListAsync_Returns_Paged_Results()
        {
            // arrange
            var db = Ctx;
            var partner = new Partner { Title = "P-1", Role = PartnerRole.CUSTOMER };
            db.Partners.Add(partner);
            await db.SaveChangesAsync();

            for (int i = 1; i <= 55; i++)
            {
                db.Documents.Add(new Document { Type = DocumentType.SALES_INVOICE, Number = $"INV-{i}", Date = DateTime.Today.AddDays(-i), Status = DocumentStatus.DRAFT, PartnerId = partner.Id });
            }
            await db.SaveChangesAsync();

            var queries = new DocumentQueries(db);

            var filter = new DocumentListFilter { PageSize = 20 };

            // act
            var p1 = await queries.ListAsync(filter, 1, 20);
            var p2 = await queries.ListAsync(filter, 2, 20);
            var p3 = await queries.ListAsync(filter, 3, 20);

            // assert
            Assert.Equal(55, p1.TotalCount);
            Assert.Equal(20, p1.Items.Count);
            Assert.Equal(20, p2.Items.Count);
            Assert.Equal(15, p3.Items.Count);
        }
    }
}

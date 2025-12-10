using System.Threading.Tasks;
using Xunit;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Persistence;
using Tests.Infrastructure;
using FluentAssertions;
using System.Linq;
using Moq;

namespace Tests.Integration
{
    /// <summary>
    /// TST-027: R-062 Sales Order Management Integration Tests
    /// R-161: Added UI-level tests for DocumentEditViewModel (R-159 Partner Selection, R-160 Validation)
    /// </summary>
    public class SalesOrderReservationTests : BaseIntegrationTest
    {
        [Fact]
        public async Task Approving_SalesOrder_Increases_ReservedQty_And_AvailableDecreases()
        {
            var db = Ctx;

            // seed product and initial stock
            var p = new Product { Sku = "SO1", Name = "SO Prod", BaseUom = "pcs", VatRate = 20 };
            db.Products.Add(p);
            var partner = new Partner { Name = "CUST1", PartnerType = PartnerType.Customer };
            db.Partners.Add(partner);
            await db.SaveChangesAsync();

            // add stock moves (on_hand = 10)
            db.StockMoves.Add(new Domain.Entities.StockMove { ItemId = p.Id, Date = System.DateTime.UtcNow, QtySigned = 10m });
            await db.SaveChangesAsync();

            // create sales order draft
            var docDto = new Application.Documents.DTOs.DocumentDetailDto { Type = "SALES_ORDER", Number = "SO-1", Date = System.DateTime.Today, PartnerId = partner.Id };
            docDto.Lines.Add(new Application.Documents.DTOs.DocumentLineDto { ItemId = p.Id, Qty = 3m, UnitPrice = 0m, Uom = "pcs", VatRate = 20 });

            var svc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));
            var id = await svc.CreateDraftAsync(docDto);

            // approve
            await svc.ApproveAsync(id);

            var prod = await Ctx.Products.FindAsync(p.Id);
            prod.Should().NotBeNull();
            prod!.ReservedQty.Should().Be(3m);

            var invQ = new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx);
            var onHand = await invQ.GetOnHandAsync(p.Id);
            var available = await invQ.GetAvailableAsync(p.Id);

            onHand.Should().Be(10m);
            available.Should().Be(7m);
        }

        [Fact]
        public async Task Cancelling_SalesOrder_Decreases_ReservedQty()
        {
            var db = Ctx;

            var p = new Product { Sku = "SO2", Name = "SO Prod 2", BaseUom = "pcs", VatRate = 20 };
            db.Products.Add(p);
            var partner = new Partner { Name = "CUST2", PartnerType = PartnerType.Customer };
            db.Partners.Add(partner);
            await db.SaveChangesAsync();

            db.StockMoves.Add(new Domain.Entities.StockMove { ItemId = p.Id, Date = System.DateTime.UtcNow, QtySigned = 5m });
            await db.SaveChangesAsync();

            var docDto = new Application.Documents.DTOs.DocumentDetailDto { Type = "SALES_ORDER", Number = "SO-2", Date = System.DateTime.Today, PartnerId = partner.Id };
            docDto.Lines.Add(new Application.Documents.DTOs.DocumentLineDto { ItemId = p.Id, Qty = 2m, UnitPrice = 0m, Uom = "pcs", VatRate = 20 });

            var svc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));
            var id = await svc.CreateDraftAsync(docDto);
            await svc.ApproveAsync(id);

            var prod = await Ctx.Products.FindAsync(p.Id);
            prod!.ReservedQty.Should().Be(2m);

            // cancel
            await svc.CancelAsync(id);

            var prod2 = await Ctx.Products.FindAsync(p.Id);
            prod2!.ReservedQty.Should().Be(0m);
        }

        /// <summary>
        /// R-161 (TST-027 UI): Test R-159 Partner Selection - verify Partners collection loads
        /// </summary>
        [Fact]
        public async Task SalesOrder_UI_Should_Load_Partners_For_Selection()
        {
            // Arrange: Create test partner
            var partner = new Partner { Name = "Test Customer SO", PartnerType = PartnerType.Customer };
            Ctx.Partners.Add(partner);
            await Ctx.SaveChangesAsync();

            var dto = new Application.Documents.DTOs.DocumentDetailDto
            {
                Type = "SALES_ORDER",
                Number = "SO-UI-1",
                Date = System.DateTime.Today,
                Currency = "TRY",
                Lines = new System.Collections.Generic.List<Application.Documents.DTOs.DocumentLineDto>()
            };

            var cmdSvc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));
            var productsSvc = new global::InventoryERP.Infrastructure.Queries.ProductsReadService(Ctx);
            var partnerSvc = new global::InventoryERP.Infrastructure.Partners.PartnerService(Ctx);

            // Act: Create DocumentEditViewModel (should trigger LoadPartnersAsync)
            var vm = new InventoryERP.Presentation.ViewModels.DocumentEditViewModel(
                dto, cmdSvc, productsSvc, 
                new Tests.Unit.TestHelpers.StubDialogService(), 
                null, null, partnerSvc);

            // Wait for async partner load
            await Task.Delay(100);

            // Assert: Partners collection should be loaded with customers
            vm.Partners.Should().NotBeEmpty("R-159: SALES_ORDER should load Partners collection");
            vm.Partners.Should().Contain(p => p.Name == "Test Customer SO", 
                "R-159: Customer partners should be available for selection");
        }

        /// <summary>
        /// R-161 (TST-027 UI): Test R-160 Validation - SaveCommand.CanExecute when partner not selected
        /// </summary>
        [Fact]
        public async Task SalesOrder_UI_SaveCommand_Should_Be_Disabled_When_Partner_Not_Selected()
        {
            // Arrange
            var product = new Product { Sku = "SO-UI-2", Name = "Test Prod", BaseUom = "pcs", VatRate = 20 };
            Ctx.Products.Add(product);
            await Ctx.SaveChangesAsync();

            var dto = new Application.Documents.DTOs.DocumentDetailDto
            {
                Type = "SALES_ORDER",
                Number = "SO-UI-2",
                Date = System.DateTime.Today,
                Currency = "TRY",
                PartnerId = null,  // R-160: No partner selected
                Lines = new System.Collections.Generic.List<Application.Documents.DTOs.DocumentLineDto>()
            };

            dto.Lines.Add(new Application.Documents.DTOs.DocumentLineDto 
            { 
                ItemId = product.Id, 
                Qty = 1m, 
                UnitPrice = 10m, 
                Uom = "pcs", 
                VatRate = 20 
            });

            var cmdSvc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));
            var productsSvc = new global::InventoryERP.Infrastructure.Queries.ProductsReadService(Ctx);

            // Act: Create ViewModel without partner
            var vm = new InventoryERP.Presentation.ViewModels.DocumentEditViewModel(
                dto, cmdSvc, productsSvc, 
                new Tests.Unit.TestHelpers.StubDialogService());

            // Assert: SaveCommand.CanExecute should return false
            vm.SaveCommand.CanExecute(null).Should().BeFalse(
                "R-160: SaveCommand.CanExecute must return false when partner is required but not selected");
        }

        /// <summary>
        /// R-161 (TST-027 UI): Test R-160 Validation - SaveCommand.CanExecute when partner selected
        /// </summary>
        [Fact]
        public async Task SalesOrder_UI_SaveCommand_Should_Be_Enabled_When_Partner_Selected()
        {
            // Arrange
            var partner = new Partner { Name = "CUST3", PartnerType = PartnerType.Customer };
            var product = new Product { Sku = "SO-UI-3", Name = "Test Prod 3", BaseUom = "pcs", VatRate = 20 };
            Ctx.Partners.Add(partner);
            Ctx.Products.Add(product);
            await Ctx.SaveChangesAsync();

            var dto = new Application.Documents.DTOs.DocumentDetailDto
            {
                Type = "SALES_ORDER",
                Number = "SO-UI-3",
                Date = System.DateTime.Today,
                Currency = "TRY",
                PartnerId = partner.Id,  // R-160: Partner selected
                Lines = new System.Collections.Generic.List<Application.Documents.DTOs.DocumentLineDto>()
            };

            dto.Lines.Add(new Application.Documents.DTOs.DocumentLineDto 
            { 
                ItemId = product.Id, 
                Qty = 1m, 
                UnitPrice = 10m, 
                Uom = "pcs", 
                VatRate = 20 
            });

            var cmdSvc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));
            var productsSvc = new global::InventoryERP.Infrastructure.Queries.ProductsReadService(Ctx);

            // Act: Create ViewModel with partner
            var vm = new InventoryERP.Presentation.ViewModels.DocumentEditViewModel(
                dto, cmdSvc, productsSvc, 
                new Tests.Unit.TestHelpers.StubDialogService());

            // Assert: SaveCommand.CanExecute should return true
            vm.SaveCommand.CanExecute(null).Should().BeTrue(
                "R-160: SaveCommand.CanExecute must return true when partner is selected and lines exist");
        }

        /// <summary>
        /// R-161 (TST-027 UI): Test R-160 Validation - Changing PartnerId updates SaveCommand.CanExecute
        /// </summary>
        [Fact]
        public async Task SalesOrder_UI_SaveCommand_CanExecute_Should_Update_When_PartnerId_Changes()
        {
            // Arrange
            var partner = new Partner { Name = "CUST4", PartnerType = PartnerType.Customer };
            var product = new Product { Sku = "SO-UI-4", Name = "Test Prod 4", BaseUom = "pcs", VatRate = 20 };
            Ctx.Partners.Add(partner);
            Ctx.Products.Add(product);
            await Ctx.SaveChangesAsync();

            var dto = new Application.Documents.DTOs.DocumentDetailDto
            {
                Type = "SALES_ORDER",
                Number = "SO-UI-4",
                Date = System.DateTime.Today,
                Currency = "TRY",
                PartnerId = null,  // Start with no partner
                Lines = new System.Collections.Generic.List<Application.Documents.DTOs.DocumentLineDto>()
            };

            dto.Lines.Add(new Application.Documents.DTOs.DocumentLineDto 
            { 
                ItemId = product.Id, 
                Qty = 1m, 
                UnitPrice = 10m, 
                Uom = "pcs", 
                VatRate = 20 
            });

            var cmdSvc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));
            var productsSvc = new global::InventoryERP.Infrastructure.Queries.ProductsReadService(Ctx);
            var vm = new InventoryERP.Presentation.ViewModels.DocumentEditViewModel(
                dto, cmdSvc, productsSvc, 
                new Tests.Unit.TestHelpers.StubDialogService());

            // Assert 1: Initially disabled (no partner)
            vm.SaveCommand.CanExecute(null).Should().BeFalse("Initially no partner selected");

            // Act: Set PartnerId
            vm.PartnerId = partner.Id;

            // Assert 2: Now enabled (partner selected)
            vm.SaveCommand.CanExecute(null).Should().BeTrue(
                "R-160: SaveCommand.CanExecute must update when PartnerId changes from null to valid value");

            // Act 2: Clear PartnerId
            vm.PartnerId = null;

            // Assert 3: Disabled again
            vm.SaveCommand.CanExecute(null).Should().BeFalse(
                "R-160: SaveCommand.CanExecute must update when PartnerId changes from valid to null");
        }
    }
}

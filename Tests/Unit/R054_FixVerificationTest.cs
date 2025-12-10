using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using FluentAssertions;
using Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;

namespace Tests.Unit;

/// <summary>
/// R-054 FIX VERIFICATION TEST
/// Purpose: Verify that SourceLocationId is preserved when editing a pre-populated document line.
/// 
/// ROOT CAUSE (Discovered via R-053 Diagnostic):
/// - R-045: Pre-populate adjustment document with line (Qty=0, SourceLocationId=1)
/// - R-050: Set default SourceLocationId=1 on pre-populated line
/// - User edits Qty from 0 to 10
/// - BUG: DocumentQueries.GetAsync() didn't include SourceLocationId in DTO mapping
/// - Result: When document loaded for editing, SourceLocationId=null in ViewModel
/// - Result: SaveAsync() created new line with SourceLocationId=null
/// - Result: ApproveAsync() failed with FK constraint violation
/// 
/// FIX IMPLEMENTED IN R-054:
/// - Infrastructure/Queries/DocumentQueries.cs: Added SourceLocationId, DestinationLocationId, 
///   ProductVariantId, Coefficient, LotId to DTO mapping
/// - This ensures fields are preserved when document is loaded for editing
/// 
/// Test Scenario:
/// 1. Create draft adjustment with line (Qty=10, SourceLocationId=1)
/// 2. Simulate user editing: Load document via GetAsync() -> Modify Qty -> SaveAsync()
/// 3. Approve document
/// 4. Verify: StockMove created with SourceLocationId=1 (NOT null)
/// </summary>
public class R054_FixVerificationTest : IDisposable
{
    private SqliteConnection? _connection;

    [Fact]
    public async Task WhenEditingPrePopulatedLine_SourceLocationIdShouldBePreserved()
    {
        // Arrange: Setup database with test product and warehouse location
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var db = provider.GetRequiredService<Persistence.AppDbContext>();
        
        // Create test product
        var testProduct = new Domain.Entities.Product
        {
            Sku = "TEST-R054",
            Name = "Product for R-054 SourceLocationId Preservation",
            BaseUom = "EA",
            VatRate = 20,
            Active = true
        };
        db.Products.Add(testProduct);

        // Ensure LocationId=1 exists (should be created by Init.sql)
        var location = await db.Locations.FindAsync(1);
        if (location == null)
        {
            var warehouse = new Domain.Entities.Warehouse { Code = "WH01", Name = "Default Warehouse" };
            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync();
            location = new Domain.Entities.Location { Id = 1, Code = "LOC01", Name = "Default Location", WarehouseId = warehouse.Id };
            db.Locations.Add(location);
        }
        await db.SaveChangesAsync();

        var docSvc = provider.GetRequiredService<IDocumentCommandService>();
        var docQueries = provider.GetRequiredService<IDocumentQueries>();

        // Act 1: Create draft with pre-populated line (simulates R-045/R-050 behavior)
        var draftDto = new DocumentDetailDto
        {
            Type = "ADJUSTMENT_OUT",
            Number = "ADJ-R054-TEST",
            Date = DateTime.Today,
            Currency = "TRY",
            PartnerId = 0,
            Lines = new System.Collections.Generic.List<DocumentLineDto>
            {
                new DocumentLineDto
                {
                    ItemId = testProduct.Id,
                    ItemName = testProduct.Name,
                    Qty = 0, // Pre-populated with Qty=0
                    UnitPrice = 0,
                    VatRate = testProduct.VatRate,
                    Uom = testProduct.BaseUom,
                    Coefficient = 1m,
                    SourceLocationId = 1, // R-050: Default SourceLocationId
                    DestinationLocationId = null
                }
            }
        };

        int draftId = await docSvc.CreateDraftAsync(draftDto);
        draftId.Should().BeGreaterThan(0, "R-054: Draft should be created");

        // Act 2: Simulate user editing (simulates opening DocumentEditDialog)
        // R-054 FIX: DocumentQueries.GetAsync() should NOW include SourceLocationId in DTO
        var loadedDto = await docQueries.GetAsync(draftId);
        loadedDto.Should().NotBeNull("R-054: Document should load successfully");
        loadedDto!.Lines.Should().HaveCount(1, "R-054: Should have one line");
        
        // R-054 FIX VERIFICATION POINT 1: SourceLocationId should be loaded from database
        loadedDto.Lines[0].SourceLocationId.Should().Be(1, 
            "R-054 FIX: DocumentQueries.GetAsync() should include SourceLocationId in DTO mapping");

        // Act 3: Simulate user editing Qty and saving (simulates DocumentEditViewModel.SaveAsync)
        loadedDto.Lines[0].Qty = 10; // User changes Qty from 0 to 10
        await docSvc.UpdateDraftAsync(draftId, loadedDto);

        // Act 4: Approve document (triggers InvoicePostingService.ApproveAndPostAsync)
        await docSvc.ApproveAsync(draftId);

        // Assert: R-054 FIX VERIFICATION POINT 2
        // After approving, StockMove should have SourceLocationId=1 (preserved from original line)
        var stockMoves = db.StockMoves.Where(m => m.ItemId == testProduct.Id).ToList();
        stockMoves.Should().HaveCount(1, "R-054: Should create one StockMove");

        var move = stockMoves.First();
        move.SourceLocationId.Should().Be(1, 
            "R-054 FIX: SourceLocationId should be preserved when line is edited. " +
            "This verifies the fix for 'SQLite Error 19: FOREIGN KEY constraint failed' " +
            "caused by NULL SourceLocationId after user edited pre-populated line.");
        move.QtySigned.Should().Be(-10, "R-054: ADJUSTMENT_OUT should reduce stock");
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}

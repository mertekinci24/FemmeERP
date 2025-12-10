using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Stocks;
using FluentAssertions;
using Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;

namespace Tests.Unit;

/// <summary>
/// R-047 FIX VERIFICATION TEST
/// Purpose: Verify that after creating and approving an Adjustment Slip, the resulting StockMove records are visible.
/// 
/// UAT Report (v1.3.10):
/// - R-046 SUCCESS: Adjustment Slip saves successfully (PartnerId FK fix)
/// - R-047 FAIL: Right-click "Stok Hareketleri" dialog showed empty
/// - R-049 FAIL: Main "Stok & Depo" -> "Stok Hareketleri" tab showed empty
/// 
/// Root Cause: DRAFT documents don't create StockMoves. Only APPROVED/POSTED documents create StockMoves.
/// Fix: Auto-approve ADJUSTMENT documents on save (in StocksViewModel.CreateAdjustmentDocumentAsync).
/// 
/// Test verifies:
/// 1. Document approval triggers InvoicePostingService.ApproveAndPostAsync
/// 2. InvoicePostingService creates StockMove records
/// 3. IStockQueries.ListMovesAsync returns the movements
/// 4. ADJUSTMENT_OUT creates negative QtySigned (reduces stock)
/// </summary>
public class R047_FixVerificationTest : IDisposable
{
    private SqliteConnection? _connection;

    [Fact]
    public async Task AfterApprovingAdjustmentSlip_StockMovementsShouldBeVisible()
    {
        // Arrange: Setup database and create test product
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var db = provider.GetRequiredService<Persistence.AppDbContext>();
        var testProduct = new Domain.Entities.Product
        {
            Sku = "TEST-R047",
            Name = "Product for R-047 Stock Movements",
            BaseUom = "EA",
            VatRate = 20,
            Active = true
        };
        db.Products.Add(testProduct);
        await db.SaveChangesAsync();

        // Act 1: Create and approve Adjustment Slip (simulates StocksViewModel.CreateAdjustmentDocumentAsync + ApproveAsync)
        var docSvc = provider.GetRequiredService<IDocumentCommandService>();
        var draftDto = new DocumentDetailDto
        {
            Type = "ADJUSTMENT_OUT",
            Number = "ADJ-R047-TEST",
            Date = DateTime.Today,
            Currency = "TRY",
            PartnerId = 0, // Will be converted to null by R-046 fix
            Lines = new System.Collections.Generic.List<DocumentLineDto>
            {
                new DocumentLineDto
                {
                    ItemId = testProduct.Id,
                    ItemName = testProduct.Name,
                    Qty = 10, // Adjustment quantity
                    UnitPrice = 0,
                    VatRate = testProduct.VatRate,
                    Uom = testProduct.BaseUom,
                    // R-050 FIX: Set default SourceLocationId (matches StocksViewModel fix)
                    SourceLocationId = 1,
                    DestinationLocationId = null
                }
            }
        };

        int draftId = await docSvc.CreateDraftAsync(draftDto);
        draftId.Should().BeGreaterThan(0, "R-047: Draft should be created");

        // Update to simulate user entering Qty and clicking Save
        await docSvc.UpdateDraftAsync(draftId, draftDto);

        // R-047 FIX: Approve the document (simulates StocksViewModel.CreateAdjustmentDocumentAsync calling ApproveAsync)
        // This triggers InvoicePostingService.ApproveAndPostAsync which creates StockMove records
        await docSvc.ApproveAsync(draftId);

        // Act 2: Query StockMoves via IStockQueries (simulates StockMovesDialog.LoadAsync)
        var stockQueries = provider.GetRequiredService<IStockQueries>();
        var movements = await stockQueries.ListMovesAsync(testProduct.Id, null, null);

        // Assert: R-047 FIX VERIFICATION
        // After approving ADJUSTMENT document, StockMoves should be created and visible
        movements.Should().NotBeNull("R-047: Query should return a collection");
        movements.Count.Should().BeGreaterThan(0, 
            "R-047 FIX: After approving adjustment slip, stock movements should be visible via IStockQueries");

        var firstMove = movements.FirstOrDefault();
        firstMove.Should().NotBeNull("R-047: Should have at least one movement");
        // R-047 FIX VERIFIED: ADJUSTMENT_OUT creates negative QtySigned (reduces stock)
        firstMove!.Qty.Should().Be(-10, "R-047: ADJUSTMENT_OUT movement should be negative (write-off reduces stock)");
        firstMove.DocType.Should().Contain("ADJUSTMENT", "R-047: Doc type should be ADJUSTMENT_OUT");
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}

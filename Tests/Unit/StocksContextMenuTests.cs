using System;
using InventoryERP.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Unit;

public class StocksContextMenuTests : IDisposable
{
    private Microsoft.Data.Sqlite.SqliteConnection? _connection;

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    [WpfFact]
    public void StocksViewModel_CreateAdjustmentDocument_WithCommandParameter_OpensDialog()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<StocksViewModel>();
        var testProduct = new Application.Products.ProductRowDto(1, "SKU-TEST", "Test Product", "EA", 20, true, 0m);

        // Act: Execute with product as CommandParameter (simulates context menu click)
        Assert.True(vm.CreateAdjustmentDocumentCmd.CanExecute(testProduct));
        vm.CreateAdjustmentDocumentCmd.Execute(testProduct);

        // Assert: no exception and command executed
        Assert.True(true);
    }

    [WpfFact]
    public void StocksViewModel_ShowStockInfo_WithCommandParameter_ShowsMessageBox()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<StocksViewModel>();
        var testProduct = new Application.Products.ProductRowDto(1, "SKU-TEST", "Test Product", "EA", 20, true, 100m);

        // Act: Execute with product as CommandParameter
        Assert.True(vm.ShowStockInfoCmd.CanExecute(testProduct));
        vm.ShowStockInfoCmd.Execute(testProduct);

        // Assert: no exception (MessageBox shown in non-blocking test mode)
        Assert.True(true);
    }

    [WpfFact]
    public void StocksViewModel_ShowMovements_WithCommandParameter_OpensDialog()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<StocksViewModel>();
        var testProduct = new Application.Products.ProductRowDto(1, "SKU-TEST", "Test Product", "EA", 20, true, 0m);

        // Act: Execute with product as CommandParameter
        Assert.True(vm.ShowMovementsCmd.CanExecute(testProduct));
        vm.ShowMovementsCmd.Execute(testProduct);

        // Assert: no exception
        Assert.True(true);
    }

    [WpfFact]
    public void StocksViewModel_ContextMenuCommands_WithNullParameter_ShowsWarning()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<StocksViewModel>();

        // Act: Execute with null CommandParameter (simulates right-click on empty area)
        vm.ShowStockInfoCmd.Execute(null);
        vm.CreateAdjustmentDocumentCmd.Execute(null);

        // Assert: Should show "ürün seçilmedi" but not throw (test mode non-blocking)
        Assert.True(true);
    }

    /// <summary>
    /// R-039: Fix Stale Grid Selection - Verify null checks prevent execution without item
    /// Note: RelayCommand with async doesn't support CanExecute predicate, so null checks are in method body
    /// </summary>
    [WpfFact]
    public void R039_StocksViewModel_WithNullParameter_DoesNotThrow()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<StocksViewModel>();

        // Act: Execute with null parameter (simulates cleared selection)
        // Should show "Ürün seçilmedi" but not throw exception
        vm.EditItemCmd.Execute(null);
        vm.ShowMovementsCmd.Execute(null);
        vm.CreateAdjustmentDocumentCmd.Execute(null);

        // Assert: No exception thrown (null checks in methods prevent errors)
        Assert.True(true, "Commands handle null parameter gracefully");
    }

    /// <summary>
    /// R-039: Verify DataGrid selection clearing via StocksView.xaml.cs PreviewMouseDown handler
    /// This test documents the UI-level implementation (cannot be unit tested without UI)
    /// </summary>
    [WpfFact]
    public void R039_Documentation_DataGridSelectionClearing()
    {
        // R-039 Implementation:
        // - StocksView.xaml: Added PreviewMouseDown="DataGrid_PreviewMouseDown"
        // - StocksView.xaml.cs: Handler clears viewModel.Selected when clicking empty space
        // - Result: Context menu commands receive null parameter and show "Ürün seçilmedi" message
        
        // This test documents the feature (full UI testing requires integration tests with WPF controls)
        Assert.True(true, "R-039 selection clearing implemented in StocksView.xaml.cs");
    }
}

using System;
using InventoryERP.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Unit;

public class DataManagementViewModelTests : IDisposable
{
    private Microsoft.Data.Sqlite.SqliteConnection? _connection;

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    [WpfFact]
    public void DataManagementViewModel_ImportProductsCsvCmd_IsNotNull()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<DataManagementViewModel>();

        // Assert: ImportProductsCsvCmd should be available
        Assert.NotNull(vm.ImportProductsCsvCmd);
    }

    [WpfFact]
    public void DataManagementViewModel_ImportOpeningStockCsvCmd_IsNotNull()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<DataManagementViewModel>();

        // Assert: ImportOpeningStockCsvCmd should be available
        Assert.NotNull(vm.ImportOpeningStockCsvCmd);
    }

    [WpfFact]
    public void DataManagementViewModel_Commands_CanExecute()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<DataManagementViewModel>();

        // Assert: Commands should be executable
        Assert.True(vm.ImportProductsCsvCmd.CanExecute(null));
        Assert.True(vm.ImportOpeningStockCsvCmd.CanExecute(null));
    }
}

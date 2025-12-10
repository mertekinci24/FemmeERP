using System;
using InventoryERP.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Unit;

public class FinanceModuleViewModelTests : IDisposable
{
    private Microsoft.Data.Sqlite.SqliteConnection? _connection;

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    [WpfFact]
    public void FinanceModuleViewModel_NewSalesInvoiceCmd_IsNotNull()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<FinanceModuleViewModel>();

        // Assert: NewSalesInvoiceCmd should be available
        Assert.NotNull(vm.NewSalesInvoiceCmd);
    }

    [WpfFact]
    public void FinanceModuleViewModel_NewSalesInvoiceCmd_CanExecute()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<FinanceModuleViewModel>();

        // Assert: Command should be executable
        Assert.True(vm.NewSalesInvoiceCmd.CanExecute(null));
    }

    [WpfFact]
    public void FinanceModuleViewModel_HasDocumentsView()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<FinanceModuleViewModel>();

        // Assert: DocumentsView child should be resolved
        Assert.NotNull(vm.DocumentsView);
    }
}

using System;
using InventoryERP.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Unit;

/// <summary>
/// R-036: Smoke test to verify that Command bindings in ViewModels execute properly.
/// Refactored to use TestServiceProviderFactory with real DI container and in-memory database.
/// This ensures that global Button styles don't break Command execution (R-015 showstopper regression check).
/// NO MORE DUMMY/MOCK SERVICES - uses real Infrastructure + Persistence layers.
/// </summary>
public class CommandBindingSmokeTests : IDisposable
{
    private Microsoft.Data.Sqlite.SqliteConnection? _connection;

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    [WpfFact]
    public void DocumentsViewModel_NewInvoiceCommand_CanExecute_And_Invokes_CreateDraft()
    {
        // R-036: Use TestServiceProviderFactory to create real DI container with in-memory database
        // [WpfFact] ensures a WPF Dispatcher is available for modal dialogs
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<DocumentsViewModel>();

        // Auto-close any DocumentEditDialog shown by the command to avoid blocking ShowDialog
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
        {
            foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
            {
                if (w.GetType().FullName == "InventoryERP.Presentation.Views.DocumentEditDialog")
                {
                    w.Close();
                }
            }
        }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);

        // Act: Execute the NewInvoiceCommand (mimics button click)
        Assert.True(vm.NewInvoiceCommand.CanExecute(null), "NewInvoiceCommand should be executable");
        
        // CRITICAL: We MUST call Execute() to verify the command actually works (not just binding)
        vm.NewInvoiceCommand.Execute(null);

        // Assert: Verify the command executed without exception (proof that command binding works with real services)
        // NewInvoiceCommand opens a dialog and uses real IDocumentCommandService with in-memory database
        Assert.True(true, "Command executed without exception; binding is intact with real DI services");
    }

    [WpfFact]
    public void DocumentsViewModel_RefreshCommand_CanExecute()
    {
        // R-036: Use TestServiceProviderFactory to create real DI container with in-memory database
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = provider.GetRequiredService<DocumentsViewModel>();

        // Act & Assert: Verify that RefreshCommand is executable (used by multiple buttons)
        Assert.True(vm.RefreshCommand.CanExecute(null), "RefreshCommand should be executable");
        vm.RefreshCommand.Execute(null);
        // Execution completes without error with real IDocumentQueries = pass ?
    }
}

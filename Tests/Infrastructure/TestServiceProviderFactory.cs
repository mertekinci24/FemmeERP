using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Serilog;
using System;

namespace Tests.Infrastructure;

/// <summary>
/// R-036: Factory to create a ServiceProvider that mirrors production DI registration.
/// Uses TestDbContextFactory for in-memory SQLite database.
/// Replaces all "Dummy" mock services with real implementations.
/// </summary>
public static class TestServiceProviderFactory
{
    /// <summary>
    /// Creates a ServiceProvider with full Infrastructure + Persistence + Presentation registration.
    /// Uses in-memory SQLite database via TestDbContextFactory.
    /// </summary>
    public static (IServiceProvider Provider, SqliteConnection Connection) CreateWithInMemoryDb()
    {
        var services = new ServiceCollection();
        
        // R-037: Configure Serilog for test environment (silent logger)
        var logger = new LoggerConfiguration()
            .MinimumLevel.Error() // Only log errors in tests
            .CreateLogger(); // No sinks needed for tests
        services.AddSingleton<ILogger>(logger);
        
        // Configuration (mimics appsettings.json)
        var inMemorySettings = new System.Collections.Generic.Dictionary<string, string>
        {
            {"Ui:DebounceMs", "250"},
            {"ConnectionStrings:DefaultConnection", "DataSource=:memory:"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
        
        // Register UI Options (required by DocumentEditViewModel)
        services.Configure<InventoryERP.Presentation.Configuration.UiOptions>(configuration.GetSection("Ui"));
        
        // R-037: Register FakeDialogService for tests (no-op UI operations)
        services.AddSingleton<InventoryERP.Presentation.Abstractions.IDialogService, FakeDialogService>();
        
        // Create in-memory SQLite database
        var (ctx, conn) = TestDbContextFactory.Create();
        
        // Register AppDbContext as singleton with the in-memory database
        services.AddSingleton<AppDbContext>(ctx);
        services.AddSingleton<DbContext>(ctx); // Some services may depend on DbContext
        
        // Register Infrastructure services (Commands, Queries, Validators)
        // Use extension method from Infrastructure project (global:: prefix for disambiguation)
        global::InventoryERP.Infrastructure.DependencyInjection.AddInfrastructure(services);
        
        // Register Presentation ViewModels and Dialogs
        RegisterPresentationServices(services);
        
        var provider = services.BuildServiceProvider();
        return (provider, conn);
    }
    
    /// <summary>
    /// Registers all Presentation layer services (Views, ViewModels, Dialogs).
    /// Mirrors InventoryApp.xaml.cs registration.
    /// </summary>
    private static void RegisterPresentationServices(IServiceCollection services)
    {
        // Note: ShellWindow is a WPF Window and cannot be instantiated in test environment
        // We only register ViewModels that tests need
        services.AddSingleton<InventoryERP.Presentation.ShellViewModel>();
        
        // R-044.1: Accounting and Finance modules
        services.AddSingleton<InventoryERP.Presentation.Views.AccountingModuleView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.AccountingModuleViewModel>();
        services.AddSingleton<InventoryERP.Presentation.Views.FinanceModuleView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.FinanceModuleViewModel>();
        
        // R-044.2: Stock Management and Warehouse modules
        services.AddSingleton<InventoryERP.Presentation.Views.StockManagementModuleView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.StockManagementModuleViewModel>();
        services.AddSingleton<InventoryERP.Presentation.Views.WarehouseModuleView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.WarehouseModuleViewModel>();
    services.AddSingleton<InventoryERP.Presentation.Views.WarehouseManagementView>();
    services.AddSingleton<InventoryERP.Presentation.ViewModels.WarehouseManagementViewModel>();
    services.AddSingleton<InventoryERP.Presentation.Views.StockModuleView>();
    services.AddSingleton<InventoryERP.Presentation.ViewModels.StockModuleViewModel>();
        
        // Production module
        services.AddSingleton<InventoryERP.Presentation.Views.ProductionModuleView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.ProductionModuleViewModel>();

    // Sales module
    services.AddSingleton<InventoryERP.Presentation.Views.SalesModuleView>();
    services.AddSingleton<InventoryERP.Presentation.ViewModels.SalesModuleViewModel>();
        
        // Data Management module
        services.AddSingleton<InventoryERP.Presentation.Views.DataManagementView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.DataManagementViewModel>();
        
        // Child views (used by modules)
        services.AddSingleton<InventoryERP.Presentation.Views.DocumentsView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.DocumentsViewModel>();
        services.AddSingleton<InventoryERP.Presentation.Views.StocksView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.StocksViewModel>();
        services.AddSingleton<InventoryERP.Presentation.Views.PartnersView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.PartnersViewModel>();
        services.AddSingleton<InventoryERP.Presentation.Views.ReportsView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.ReportsViewModel>();
    services.AddSingleton<InventoryERP.Presentation.Views.CashAccountListView>();
    services.AddSingleton<InventoryERP.Presentation.ViewModels.Cash.CashAccountListViewModel>();
        
        // Dashboard
        services.AddSingleton<InventoryERP.Presentation.Views.DashboardView>();
        services.AddSingleton<InventoryERP.Presentation.ViewModels.DashboardViewModel>();
        
        // Settings
        services.AddSingleton<InventoryERP.Presentation.Views.SettingsView>();
        
        // Transient Dialogs (new instance per request)
    services.AddTransient<InventoryERP.Presentation.Views.DocumentEditDialog>();
    services.AddTransient<InventoryERP.Presentation.ViewModels.DocumentEditViewModel>();
    services.AddTransient<InventoryERP.Presentation.Views.CashReceiptDialog>();
    services.AddTransient<InventoryERP.Presentation.Views.CashPaymentDialog>();
    services.AddTransient<InventoryERP.Presentation.ViewModels.Cash.CashReceiptDialogViewModel>();
    services.AddTransient<InventoryERP.Presentation.ViewModels.Cash.CashPaymentDialogViewModel>();
        
        // Theme service
        services.AddSingleton<Application.Common.IThemeService, InventoryERP.Presentation.Services.ThemeService>();
    }
}

using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Presentation.Views;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using System.Linq;

namespace InventoryERP.Presentation
{
    public partial class InventoryApp : System.Windows.Application
    {
        private IHost _host;

        public InventoryApp()
        {
            // R-109: Configure QuestPDF license at application startup (required by QuestPDF)
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // R-037: Configure Serilog for diagnostic logging
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var logPath = Path.Combine(desktop, "events.log");
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, 
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            
            Log.Information("====== InventoryERP Application Started (v1.9.42_UI_SYNC) ======");
            
            // R-052: Register WPF DispatcherUnhandledException for UI thread exceptions
            this.DispatcherUnhandledException += InventoryApp_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    // R-037: Register Serilog ILogger
                    services.AddSingleton<Serilog.ILogger>(Log.Logger);
                    
                    // R-037: Register IDialogService abstraction
                    services.AddSingleton<InventoryERP.Presentation.Abstractions.IDialogService, InventoryERP.Presentation.Services.WpfDialogService>();
                    // R-008: Register IFileDialogService for PDF export (required by DocumentEditViewModel)
                    services.AddSingleton<InventoryERP.Presentation.Abstractions.IFileDialogService, InventoryERP.Presentation.Services.WpfFileDialogService>();
                    
                    services.AddSingleton<InventoryERP.Application.Common.IThemeService, InventoryERP.Presentation.Services.ThemeService>();
                    // Presentation view + vm registrations
                    services.AddSingleton(typeof(ShellWindow));
                    services.AddSingleton(typeof(ShellViewModel));

                    // Modules per new IA
                    services.AddSingleton(typeof(FinanceModuleView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.FinanceModuleViewModel));
                    services.AddSingleton(typeof(StockModuleView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.StockModuleViewModel));
                    services.AddSingleton(typeof(ProductionModuleView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.ProductionModuleViewModel));
                    // R-060: Sales & Marketing Module
                    services.AddSingleton(typeof(SalesModuleView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.SalesModuleViewModel));

                    // Child views (used by modules)
                    services.AddSingleton(typeof(DocumentsView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.DocumentsViewModel));
                    // R-060: Quotes View (child of Sales module)
                    services.AddSingleton(typeof(QuotesView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.QuotesViewModel));

                    // Other modules
                    services.AddSingleton(typeof(StocksView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.StocksViewModel));
                    services.AddSingleton(typeof(PartnersView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.PartnersViewModel));
                    // R-194.1: Register PartnerListView and ViewModel for main-area navigation
                    services.AddTransient(typeof(PartnerListView));
                    services.AddTransient(typeof(InventoryERP.Presentation.ViewModels.PartnerListViewModel));
                    services.AddSingleton(typeof(ReportsView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.ReportsViewModel));
                    services.AddSingleton(typeof(CashAccountListView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.Cash.CashAccountListViewModel));
                    
                    // R-040: Warehouse Management View
                    services.AddSingleton(typeof(WarehouseManagementView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.WarehouseManagementViewModel));
                    
                    // Dashboard
                    services.AddSingleton(typeof(DashboardView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.DashboardViewModel));
                    // Data Management (placeholder)
                    services.AddSingleton(typeof(DataManagementView));
                    services.AddSingleton(typeof(InventoryERP.Presentation.ViewModels.DataManagementViewModel));

                    // Dialogs (Transient - new instance each time)
                    services.AddTransient(typeof(Views.DocumentEditDialog));
                    services.AddTransient(typeof(InventoryERP.Presentation.ViewModels.DocumentEditViewModel));
                    services.AddTransient(typeof(Views.CashReceiptDialog));
                    services.AddTransient(typeof(InventoryERP.Presentation.ViewModels.Cash.CashReceiptDialogViewModel));
                    services.AddTransient(typeof(Views.CashPaymentDialog));
                    services.AddTransient(typeof(InventoryERP.Presentation.ViewModels.Cash.CashPaymentDialogViewModel));
                    services.AddTransient(typeof(Views.CashAccountEditDialog));
                    services.AddTransient(typeof(InventoryERP.Presentation.ViewModels.Cash.CashAccountEditDialogViewModel));
                    
                    // R-040: Item Edit Dialog is instantiated manually (not via DI) because it requires productId parameter

                    // UI configuration
                    services.Configure<InventoryERP.Presentation.Configuration.UiOptions>(ctx.Configuration.GetSection("Ui"));

                    // Infrastructure + Persistence
                    global::InventoryERP.Infrastructure.DependencyInjection.AddInfrastructure(services);
                    services.AddPersistence(ctx.Configuration);
                })
                .Build();
        }

        private void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                // R-037: Use Serilog for crash logging (replaces crash_log.txt)
                if (e.ExceptionObject is Exception ex)
                {
                    Log.Fatal(ex, "YAKALANAMAYAN GLOBAL HATA (CRASH) - Application is terminating");
                }
                else
                {
                    Log.Fatal("YAKALANAMAYAN GLOBAL HATA (CRASH) - Non-exception object: {ExceptionObject}", e.ExceptionObject);
                }
                
                Log.CloseAndFlush(); // Ensure log is written before crash
                
                MessageBox.Show("Uygulama beklenmedik bir hatayla karşılaştı. Detaylar için Masaüstündeki events.log dosyasına bakınız.", "Beklenmeyen Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // ignore logging errors to avoid secondary crashes
            }
        }

        private void InventoryApp_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                // R-052: Enhanced exception logging for P0 blocker diagnosis
                // Show FULL exception details (type, message, stack trace, inner exceptions)
                var fullException = e.Exception.ToString(); // This includes all inner exceptions
                
                Log.Error(e.Exception, "DISPATCHER UNHANDLED EXCEPTION - UI Thread");
                
                // R-052: Show full exception to user for UAT diagnosis
                var message = $"Beklenmedik bir hata oluştu:\n\n{fullException}\n\n" +
                             $"Bu hata bilgisi geliştiriciye iletilmelidir.\n" +
                             $"Detaylar Masaüstündeki events.log dosyasına kaydedildi.";
                
                MessageBox.Show(message, "Hata - R-052 Diagnostic", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Mark as handled to prevent app crash
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "SECONDARY EXCEPTION in DispatcherUnhandledException handler");
                // Don't mark as handled - let it crash to avoid infinite loop
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
            
            // CRITICAL: Apply all pending migrations before app runs (with logging)
            using (var scope = _host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    var pending = await dbContext.Database.GetPendingMigrationsAsync();
                    if (pending.Any())
                    {
                        Serilog.Log.Information("Applying {Count} pending migrations: {Migs}", pending.Count(), string.Join(", ", pending));
                    }
                    else
                    {
                        Serilog.Log.Information("No pending migrations. Database is up-to-date.");
                    }
                    await dbContext.Database.MigrateAsync();
                    Serilog.Log.Information("Database migration completed successfully.");
                    
                    // ---------------------------------------------------------
                    // R-213: DIRECT SCHEMA INJECTION (Bypassing Seeder)
                    // The AddProductDefaults migration is MISSING its Designer file.
                    // We force the columns to exist here in OnStartup.
                    // ---------------------------------------------------------
                    Serilog.Log.Information(">>> [R-213] DIRECT SCHEMA INJECTION STARTING...");
                    
                    // ---------------------------------------------------------
                    // R-250: SCHEMA RESCUE - Fix CRITICAL missing columns FIRST
                    // Prevents crash: 'no such column: d.Description'
                    // ---------------------------------------------------------
                    Serilog.Log.Information(">>> [R-250] APPLYING MISSING SCHEMA PATCH...");
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Documents ADD COLUMN Description TEXT DEFAULT NULL;"); Serilog.Log.Information(">>> [R-250] ADDED Documents.Description column."); } catch (Exception ex) { Serilog.Log.Warning("[R-250] Description check: " + ex.Message); }
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Document ADD COLUMN Description TEXT DEFAULT NULL;"); Serilog.Log.Information(">>> [R-250] ADDED Document.Description column."); } catch { }
                    
                    // 1. Force Product Columns (try both singular and plural table names)
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Product ADD COLUMN DefaultWarehouseId INTEGER DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Product.DefaultWarehouseId"); } catch { }
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Product ADD COLUMN DefaultLocationId INTEGER DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Product.DefaultLocationId"); } catch { }
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Product ADD COLUMN Cost TEXT DEFAULT '0';"); Serilog.Log.Information(">>> APPLIED: Product.Cost"); } catch { }
                    
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN DefaultWarehouseId INTEGER DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Products.DefaultWarehouseId"); } catch { }
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN DefaultLocationId INTEGER DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Products.DefaultLocationId"); } catch { }
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN Cost TEXT DEFAULT '0';"); Serilog.Log.Information(">>> APPLIED: Products.Cost"); } catch { }
                    
                    // 2. Force Document Columns
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Document ADD COLUMN SourceWarehouseId INTEGER DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Document.SourceWarehouseId"); } catch { }
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Document ADD COLUMN DestinationWarehouseId INTEGER DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Document.DestinationWarehouseId"); } catch { }
                    
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Documents ADD COLUMN SourceWarehouseId INTEGER DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Documents.SourceWarehouseId"); } catch { }
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Documents ADD COLUMN DestinationWarehouseId INTEGER DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Documents.DestinationWarehouseId"); } catch { }
                    // R-249: Description column for invoice notes
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Documents ADD COLUMN Description TEXT DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Documents.Description"); } catch { }
                    
                    // ---------------------------------------------------------
                    // R-235: FORCE SALESEPRICE COLUMN FOR PRICING
                    // ---------------------------------------------------------
                    Serilog.Log.Information(">>> [R-235] PATCHING SCHEMA FOR SALES PRICE...");
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Product ADD COLUMN SalesPrice TEXT DEFAULT '0';"); Serilog.Log.Information(">>> APPLIED: Product.SalesPrice"); } catch { }
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN SalesPrice TEXT DEFAULT '0';"); Serilog.Log.Information(">>> APPLIED: Products.SalesPrice"); } catch { }
                    
                    // ---------------------------------------------------------
                    // R-274: FORCE BRAND COLUMN FOR ENTERPRISE GRID
                    // ---------------------------------------------------------
                    Serilog.Log.Information(">>> [R-274] PATCHING SCHEMA FOR BRAND...");
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Product ADD COLUMN Brand TEXT DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Product.Brand"); } catch { }
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN Brand TEXT DEFAULT NULL;"); Serilog.Log.Information(">>> APPLIED: Products.Brand"); } catch { }
                    
                    // ---------------------------------------------------------
                    // R-237: FORCE DOCUMENT LINE COLUMNS FOR VAT/DISCOUNT
                    // ---------------------------------------------------------
                    Serilog.Log.Information(">>> [R-237] PATCHING DOCUMENT LINES...");
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE DocumentLine ADD COLUMN DiscountAmount TEXT DEFAULT '0';"); Serilog.Log.Information(">>> APPLIED: DocumentLine.DiscountAmount"); } catch { }
                    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE DocumentLines ADD COLUMN DiscountAmount TEXT DEFAULT '0';"); Serilog.Log.Information(">>> APPLIED: DocumentLines.DiscountAmount"); } catch { }
                    
                    Serilog.Log.Information(">>> [R-213/R-235/R-237] DIRECT SCHEMA INJECTION COMPLETED.");
                    // ---------------------------------------------------------

                    // Post-migration verification: log applied and pending migrations again
                    var appliedNow = await dbContext.Database.GetAppliedMigrationsAsync();
                    var pendingNow = await dbContext.Database.GetPendingMigrationsAsync();
                    Serilog.Log.Information("Applied migrations ({Count}): {List}", appliedNow.Count(), string.Join(", ", appliedNow));
                    Serilog.Log.Information("Pending migrations after migrate ({Count}): {List}", pendingNow.Count(), string.Join(", ", pendingNow));

                    // Enterprise diagnostic: log DB path and Partner columns
                    var cs = dbContext.Database.GetDbConnection().ConnectionString;
                    Serilog.Log.Information("DB ConnectionString: {cs}", cs);
                    var conn = dbContext.Database.GetDbConnection();
                    await conn.OpenAsync();
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            // Query attached databases
                            cmd.CommandText = "PRAGMA database_list";
                            using (var dblist = await cmd.ExecuteReaderAsync())
                            {
                                while (await dblist.ReadAsync())
                                {
                                    var seq = dblist.GetInt32(0);
                                    var name = dblist.GetString(1);
                                    var file = dblist.GetString(2);
                                    Serilog.Log.Information("Attached DB {seq}:{name} => {file}", seq, name, file);
                                }
                            } // dblist disposed here before reusing cmd

                            // Query Partner table columns
                            cmd.CommandText = "PRAGMA table_info('Partner')";
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                var cols = new System.Collections.Generic.List<string>();
                                while (await reader.ReadAsync())
                                {
                                    var cname = reader.GetString(reader.GetOrdinal("name"));
                                    var ctype = reader.GetString(reader.GetOrdinal("type"));
                                    cols.Add($"{cname}({ctype})");
                                }
                                Serilog.Log.Information("Partner columns: {cols}", string.Join(", ", cols));
                            }
                        }
                    }
                    finally
                    {
                        await conn.CloseAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log and show a friendly message; let the app continue so user can see error details
                    Serilog.Log.Error(ex, "Database migration failed at startup");
                    MessageBox.Show(
                        "Veritabanı şema güncellemesi (migration) başarısız oldu. Uygulamayı kapatıp tekrar açmayı deneyin. Sorun devam ederse geliştiriciye iletin.\n\nDetay: " + ex.Message,
                        "Migration Hatası",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            
            var shell = _host.Services.GetRequiredService<ShellWindow>();
            var shellViewModel = _host.Services.GetRequiredService<ShellViewModel>();
            shell.DataContext = shellViewModel;
            // Set initial view on UI thread so WPF controls are constructed on STA
            shellViewModel.NavigateDashboard.Execute(null);
            shell.Show();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }
    }
}

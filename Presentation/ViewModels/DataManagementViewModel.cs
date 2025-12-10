// ReSharper disable once All
#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using InventoryERP.Presentation.Commands;

namespace InventoryERP.Presentation.ViewModels
{
    /// <summary>
    /// Veri YÃ¶netimi: CSV import/export and data maintenance operations
    /// </summary>
    public class DataManagementViewModel : ViewModelBase, InventoryERP.Presentation.Actions.IContextualActions
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Serilog.ILogger _logger;

        // Import/Export commands
        public RelayCommand ImportProductsCsvCmd { get; }
        public RelayCommand ImportOpeningStockCsvCmd { get; }
        public ICommand? ImportPartnersCmd => null;
        public ICommand? ExportProductsCmd => null;

        public DataManagementViewModel(IServiceScopeFactory scopeFactory, Serilog.ILogger logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ImportProductsCsvCmd = new RelayCommand(async _ => await ImportProductsCsvAsync());
            ImportOpeningStockCsvCmd = new RelayCommand(async _ => await ImportOpeningStockCsvAsync());
        }

        private async Task ImportProductsCsvAsync()
        {
            try
            {
                _logger.Information("R-037: CSV Import iÅŸlemi baÅŸlatÄ±ldÄ± (Product Master Import) from DataManagement module");
                
                _logger.Information("R-037: OpenFileDialog aÃ§Ä±lÄ±yor...");
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    Title = "ÃœrÃ¼n CSV DosyasÄ±nÄ± SeÃ§in" // R-033: Product Master Import
                };
                var res = dlg.ShowDialog();
                
                if (res != true || string.IsNullOrWhiteSpace(dlg.FileName))
                {
                    _logger.Information("R-037: OpenFileDialog iptal edildi veya dosya seÃ§ilmedi");
                    return;
                }
                
                _logger.Information("R-037: Dosya seÃ§ildi: {FileName}", dlg.FileName);

                // v1.0.19: Store filename and dispose dialog immediately to release any file locks
                var fileName = dlg.FileName;
                _logger.Debug("R-037: Dialog reference'Ä± serbest bÄ±rakÄ±lÄ±yor (dlg = null)");
                dlg = null; // Release dialog reference
                
                _logger.Information("R-037: ImportService Ã§aÄŸrÄ±lÄ±yor...");
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<InventoryERP.Application.Import.IImportService>();
                var count = await svc.ImportProductsFromCsvAsync(fileName);
                
                _logger.Information("R-037: Import baÅŸarÄ±lÄ±. {Count} Ã¼rÃ¼n iÃ§e aktarÄ±ldÄ±", count);
                System.Windows.MessageBox.Show($"{count} Ã¼rÃ¼n iÃ§e aktarÄ±ldÄ±.", "ÃœrÃ¼nleri Ä°Ã§e Aktar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "R-037: CSV Import ViewModel'de hata oluÅŸtu!");
                System.Windows.MessageBox.Show($"Import failed: {ex.Message}", "ÃœrÃ¼nleri Ä°Ã§e Aktar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task ImportOpeningStockCsvAsync()
        {
            try
            {
                _logger.Information("R-037: Opening Stock Import iÅŸlemi baÅŸlatÄ±ldÄ± from DataManagement module");
                
                _logger.Information("R-037: OpenFileDialog aÃ§Ä±lÄ±yor (Opening Stock)...");
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    Title = "Stok AÃ§Ä±lÄ±ÅŸ CSV DosyasÄ±nÄ± SeÃ§in"
                };
                var res = dlg.ShowDialog();
                
                if (res != true || string.IsNullOrWhiteSpace(dlg.FileName))
                {
                    _logger.Information("R-037: OpenFileDialog iptal edildi (Opening Stock)");
                    return;
                }
                
                _logger.Information("R-037: Dosya seÃ§ildi (Opening Stock): {FileName}", dlg.FileName);

                // v1.0.19: Store filename and dispose dialog immediately to release any file locks
                var fileName = dlg.FileName;
                _logger.Debug("R-037: Dialog reference serbest bÄ±rakÄ±lÄ±yor (Opening Stock)");
                dlg = null; // Release dialog reference
                
                _logger.Information("R-037: ImportService.ImportOpeningBalancesFromCsvAsync Ã§aÄŸrÄ±lÄ±yor...");
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<InventoryERP.Application.Import.IImportService>();
                var count = await svc.ImportOpeningBalancesFromCsvAsync(fileName);
                
                _logger.Information("R-037: Opening Stock import baÅŸarÄ±lÄ±. {Count} stok hareketi iÃ§e aktarÄ±ldÄ±", count);
                System.Windows.MessageBox.Show($"{count} stok hareketi iÃ§e aktarÄ±ldÄ±.", "Stok AÃ§Ä±lÄ±ÅŸÄ±", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "R-037: Opening Stock Import ViewModel'de hata oluÅŸtu!");
                System.Windows.MessageBox.Show($"Import failed: {ex.Message}", "Stok AÃ§Ä±lÄ±ÅŸÄ±", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // Contextual actions (none for now)
        System.Windows.Input.ICommand? InventoryERP.Presentation.Actions.IContextualActions.NewCommand => null;
        System.Windows.Input.ICommand? InventoryERP.Presentation.Actions.IContextualActions.ExportCommand => null;
        System.Windows.Input.ICommand? InventoryERP.Presentation.Actions.IContextualActions.FiltersPreviewCommand => null;
    }
}




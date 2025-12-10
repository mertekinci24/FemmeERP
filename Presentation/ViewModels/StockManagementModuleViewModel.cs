// ReSharper disable once All
#nullable enable
using System;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.ViewModels
{
    /// <summary>
    /// R-044.2: Stock Management Module - orchestrates stock cards and movements
    /// Focus: Item master data, inventory tracking, variants, barcodes
    /// </summary>
    public class StockManagementModuleViewModel : ViewModelBase, InventoryERP.Presentation.Actions.IContextualActions
    {
        private readonly IServiceProvider _serviceProvider;

        // Child views
        public object StocksView { get; }
        public object DocumentsView { get; }

        // R-044.2: Contextual commands for Stock Management
        public ICommand NewStockCardCmd { get; }
        public ICommand NewVariantCmd { get; }
        public ICommand PrintBarcodeCmd { get; }
        public ICommand PhysicalCountCmd { get; }

        public StockManagementModuleViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Resolve child views
            StocksView = _serviceProvider.GetRequiredService<Views.StocksView>();
            DocumentsView = _serviceProvider.GetRequiredService<Views.DocumentsView>();

            // Initialize commands
            NewStockCardCmd = new Commands.RelayCommand(async _ => await NewStockCardAsync());
            NewVariantCmd = new Commands.RelayCommand(_ => NewVariant());
            PrintBarcodeCmd = new Commands.RelayCommand(_ => PrintBarcode());
            PhysicalCountCmd = new Commands.RelayCommand(_ => PhysicalCount());
        }

        private async System.Threading.Tasks.Task NewStockCardAsync()
        {
            // Delegate to StocksViewModel
            if (StocksView is System.Windows.FrameworkElement fe && 
                fe.DataContext is ViewModels.StocksViewModel stocksVm)
            {
                if (stocksVm.NewStockCmd.CanExecute(null))
                {
                    stocksVm.NewStockCmd.Execute(null);
                }
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void NewVariant()
        {
            // TODO: R-044.2 - Implement variant management
            // Right-click context: "Varyant TanÄ±mla" - color, size, etc.
            System.Windows.MessageBox.Show("Varyant TanÄ±mlama (TODO)\n\n" +
                "Renk, beden, malzeme gibi Ã¶zelliklere gÃ¶re Ã¼rÃ¼n varyantlarÄ± oluÅŸturulacak.\n" +
                "Her varyantÄ±n ayrÄ± stok takibi yapÄ±lacak.", 
                "Bilgi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void PrintBarcode()
        {
            // TODO: R-044.2 - Implement barcode printing
            // Right-click context: "Barkod YazdÄ±r"
            System.Windows.MessageBox.Show("Barkod YazdÄ±rma (TODO)\n\n" +
                "SeÃ§ili Ã¼rÃ¼nler iÃ§in EAN-13, Code128, GS1-128 formatÄ±nda\n" +
                "barkod etiketleri oluÅŸturulacak ve yazdÄ±rÄ±lacak.", 
                "Bilgi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void PhysicalCount()
        {
            // TODO: R-044.2 - Implement physical inventory count
            // Creates count sheet, allows entry of actual quantities, generates adjustment document
            System.Windows.MessageBox.Show("Stok SayÄ±mÄ± (TODO)\n\n" +
                "Depodaki fiili sayÄ±m yapÄ±lacak.\n" +
                "SayÄ±m sonucu ile sistem stoÄŸu arasÄ±ndaki farklar\n" +
                "iÃ§in otomatik dÃ¼zeltme fiÅŸi oluÅŸturulacak.", 
                "Bilgi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        // IContextualActions implementation
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.NewCommand => NewStockCardCmd;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.ExportCommand => null;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.FiltersPreviewCommand => null;
    }
}


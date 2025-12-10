// ReSharper disable once All
#nullable enable
using System;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.ViewModels
{
    /// <summary>
    /// R-044.2: Warehouse Module - orchestrates warehouse operations and location management
    /// Focus: Dispatch, receipt, transfers, location management, physical operations
    /// </summary>
    public class WarehouseModuleViewModel : ViewModelBase, InventoryERP.Presentation.Actions.IContextualActions
    {
        private readonly IServiceProvider _serviceProvider;

        // Child views
        public object DocumentsView { get; }
        public object WarehouseCardsView { get; }

        // R-044.2: Contextual commands for Warehouse Management
        public ICommand NewDispatchCmd { get; }
        public ICommand NewReceiptCmd { get; }
        public ICommand NewTransferCmd { get; }
        public ICommand NewAdjustmentCmd { get; }

        public WarehouseModuleViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Resolve child views
            DocumentsView = _serviceProvider.GetRequiredService<Views.DocumentsView>();
            
            // For warehouse cards, we can use a placeholder or create a dedicated view later
            // For now, using a simple TextBlock as placeholder
            WarehouseCardsView = new System.Windows.Controls.TextBlock
            {
                Text = "Depo ve Lokasyon KartlarÄ± (TODO)\n\n" +
                       "Bu ekranda:\n" +
                       "â€¢ Depo tanÄ±mlarÄ±\n" +
                       "â€¢ Lokasyon/Raf yapÄ±sÄ±\n" +
                       "â€¢ Depo kapasiteleri\n" +
                       "â€¢ Hacim ve kontrol bilgileri\n\n" +
                       "gÃ¶sterilecek.",
                FontSize = 14,
                Margin = new System.Windows.Thickness(20)
            };

            // Initialize commands
            NewDispatchCmd = new Commands.RelayCommand(_ => NewDispatch());
            NewReceiptCmd = new Commands.RelayCommand(_ => NewReceipt());
            NewTransferCmd = new Commands.RelayCommand(async _ => await NewTransferAsync());
            NewAdjustmentCmd = new Commands.RelayCommand(_ => NewAdjustment());
        }

        private void NewDispatch()
        {
            // TODO: R-044.2 - Implement dispatch note (sevk irsaliyesi)
            System.Windows.MessageBox.Show("Sevk Ä°rsaliyesi (TODO)\n\n" +
                "SatÄ±ÅŸ sipariÅŸlerinden otomatik sevk irsaliyesi oluÅŸturulacak.\n" +
                "FEFO/FIFO kurallarÄ±yla lot seÃ§imi yapÄ±lacak.\n" +
                "Stok otomatik dÃ¼ÅŸecek.", 
                "Bilgi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void NewReceipt()
        {
            // TODO: R-044.2 - Implement goods receipt (mal kabul)
            System.Windows.MessageBox.Show("Mal Kabul Ä°rsaliyesi (TODO)\n\n" +
                "SatÄ±n alma sipariÅŸlerinden otomatik mal kabul yapÄ±lacak.\n" +
                "Kalite kontrol ve onay mekanizmasÄ± eklenecek.\n" +
                "Stok otomatik artacak.", 
                "Bilgi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private async System.Threading.Tasks.Task NewTransferAsync()
        {
            // Delegate to DocumentsViewModel
            if (DocumentsView is System.Windows.FrameworkElement fe && 
                fe.DataContext is ViewModels.DocumentsViewModel docsVm)
            {
                await docsVm.ExecuteNewTransferAsync();
            }
        }

        private void NewAdjustment()
        {
            // TODO: R-044.2 - Implement warehouse adjustment slip (depo dÃ¼zeltme fiÅŸi)
            // Right-click context: "Depo DÃ¼zeltme FiÅŸi"
            System.Windows.MessageBox.Show("Depo DÃ¼zeltme FiÅŸi (TODO)\n\n" +
                "Ä°rsaliye ve fatura harici stok dÃ¼zeltmeleri iÃ§in kullanÄ±lÄ±r.\n" +
                "Fire, hasar, kayÄ±p gibi durumlar iÃ§in dÃ¼zeltme fiÅŸi oluÅŸturulacak.\n" +
                "Onay mekanizmasÄ± ile kontrol edilecek.", 
                "Bilgi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        // IContextualActions implementation
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.NewCommand => NewTransferCmd;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.ExportCommand => null;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.FiltersPreviewCommand => null;
    }
}


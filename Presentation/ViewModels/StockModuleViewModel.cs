// ReSharper disable once All
#nullable enable
using System;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.ViewModels
{
    /// <summary>
    /// R-045: Stock & Warehouse Module - orchestrates stock movements and cards
    /// </summary>
    public class StockModuleViewModel : ViewModelBase, InventoryERP.Presentation.Actions.IContextualActions
    {
        private readonly IServiceProvider _serviceProvider;

		// Child views
		public object DocumentsView { get; }
		public object StocksView { get; }
		public object WarehouseManagementView { get; } // R-040

		// R-045: Contextual commands for Stock module
		public ICommand NewDispatchCmd { get; }
		public ICommand NewReceiptCmd { get; }
		public ICommand NewTransferCmd { get; }
		public ICommand NewStockCardCmd { get; }

		public StockModuleViewModel(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

			// Resolve child views - filter documents to show stock-related only
			DocumentsView = _serviceProvider.GetRequiredService<Views.DocumentsView>();
			StocksView = _serviceProvider.GetRequiredService<Views.StocksView>();
			WarehouseManagementView = _serviceProvider.GetRequiredService<Views.WarehouseManagementView>(); // R-040            // Initialize commands
            NewDispatchCmd = new Commands.RelayCommand(_ => NewDispatch());
            NewReceiptCmd = new Commands.RelayCommand(_ => NewReceipt());
            NewTransferCmd = new Commands.RelayCommand(async _ => await NewTransferAsync());
            NewStockCardCmd = new Commands.RelayCommand(_ => NewStockCard());
        }

        private void NewDispatch()
        {
            System.Windows.MessageBox.Show("Yeni Sevk Ä°rsaliyesi (TODO)", "Bilgi", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void NewReceipt()
        {
            System.Windows.MessageBox.Show("Yeni Mal Kabul Ä°rsaliyesi (TODO)", "Bilgi", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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

        private void NewStockCard()
        {
            System.Windows.MessageBox.Show("Yeni Stok KartÄ± (TODO)", "Bilgi", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        // IContextualActions implementation
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.NewCommand => NewTransferCmd;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.ExportCommand => null;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.FiltersPreviewCommand => null;
    }
}


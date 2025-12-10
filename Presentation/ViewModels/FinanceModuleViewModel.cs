// ReSharper disable once All
#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using InventoryERP.Presentation.Abstractions;
using InventoryERP.Presentation.Commands;
using InventoryERP.Presentation.ViewModels.Cash;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.ViewModels
{
    /// <summary>
    /// R-044.1: Finance Module - Nakit AkÄ±ÅŸÄ± (Kasa, Tahsilat, Ã–deme)
    /// </summary>
    public class FinanceModuleViewModel : ViewModelBase, InventoryERP.Presentation.Actions.IContextualActions
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService;

    // Child views
    public object DocumentsView { get; }
    public object PartnersView { get; }
    public object CashView { get; }

        // R-044.1: Contextual commands for Finance module
        public ICommand NewSalesInvoiceCmd { get; }
        public AsyncRelayCommand NewCashReceiptCmd { get; }
        public AsyncRelayCommand NewCashPaymentCmd { get; }

        public FinanceModuleViewModel(IServiceProvider serviceProvider, IDialogService dialogService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Resolve child views
            DocumentsView = _serviceProvider.GetRequiredService<Views.DocumentsView>();
            PartnersView = _serviceProvider.GetRequiredService<Views.PartnersView>();
            CashView = _serviceProvider.GetRequiredService<Views.CashAccountListView>();

            // Initialize commands
            NewSalesInvoiceCmd = new RelayCommand(async _ => await NewSalesInvoiceAsync());
            NewCashReceiptCmd = new AsyncRelayCommand(CreateCashReceiptAsync);
            NewCashPaymentCmd = new AsyncRelayCommand(CreateCashPaymentAsync);
        }

        private async System.Threading.Tasks.Task NewSalesInvoiceAsync()
        {
            // Delegate to DocumentsViewModel
            if (DocumentsView is System.Windows.FrameworkElement fe && 
                fe.DataContext is ViewModels.DocumentsViewModel docsVm)
            {
                await docsVm.ExecuteNewInvoiceAsync();
            }
        }

        private async Task CreateCashReceiptAsync()
        {
            var result = await _dialogService.ShowCashReceiptDialogAsync();
            if (result)
            {
                await RefreshCashAccountsAsync();
            }
        }

        private async Task CreateCashPaymentAsync()
        {
            var result = await _dialogService.ShowCashPaymentDialogAsync();
            if (result)
            {
                await RefreshCashAccountsAsync();
            }
        }

        private async Task RefreshCashAccountsAsync()
        {
            if (CashView is System.Windows.FrameworkElement fe && fe.DataContext is CashAccountListViewModel vm)
            {
                await vm.RefreshAsync();
            }
        }

        // IContextualActions implementation
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.NewCommand => NewCashReceiptCmd;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.ExportCommand => NewCashPaymentCmd;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.FiltersPreviewCommand => null;
    }
}


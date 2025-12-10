// ReSharper disable once All
#nullable enable
using System;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.ViewModels
{
    /// <summary>
    /// R-044.1: Accounting Module - Faturalar ve Cari Hesaplar
    /// </summary>
    public class AccountingModuleViewModel : ViewModelBase, InventoryERP.Presentation.Actions.IContextualActions
    {
        private readonly IServiceProvider _serviceProvider;

        // Child views
        public object DocumentsView { get; }
        public object PartnersView { get; }

        // R-044.1: Contextual commands for Accounting module
        public ICommand NewSalesInvoiceCmd { get; }
        public ICommand NewPurchaseInvoiceCmd { get; }
        public ICommand NewExpenseCmd { get; }
        public ICommand NewPartnerCmd { get; }

        public AccountingModuleViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Resolve child views
            DocumentsView = _serviceProvider.GetRequiredService<Views.DocumentsView>();
            PartnersView = _serviceProvider.GetRequiredService<Views.PartnersView>();

            // Initialize commands
            NewSalesInvoiceCmd = new Commands.RelayCommand(async _ => await NewSalesInvoiceAsync());
            NewPurchaseInvoiceCmd = new Commands.RelayCommand(_ => NewPurchaseInvoice());
            NewExpenseCmd = new Commands.RelayCommand(_ => NewExpense());
            NewPartnerCmd = new Commands.RelayCommand(_ => NewPartner());
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

        private void NewPurchaseInvoice()
        {
            System.Windows.MessageBox.Show("Yeni AlÄ±ÅŸ FaturasÄ± (TODO - R-031)", "Bilgi", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void NewExpense()
        {
            System.Windows.MessageBox.Show("Yeni Gider FiÅŸi (TODO - Sabit Maliyetler)", "Bilgi", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void NewPartner()
        {
            System.Windows.MessageBox.Show("Yeni Cari Kart (TODO)", "Bilgi", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        // IContextualActions implementation
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.NewCommand => NewSalesInvoiceCmd;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.ExportCommand => null;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.FiltersPreviewCommand => null;
    }
}


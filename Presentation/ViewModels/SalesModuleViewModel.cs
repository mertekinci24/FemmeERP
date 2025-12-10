// ReSharper disable once All
#nullable enable
using System;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.ViewModels
{
    /// <summary>
    /// R-060: Sales & Marketing Module ViewModel
    /// </summary>
    public class SalesModuleViewModel : ViewModelBase, InventoryERP.Presentation.Actions.IContextualActions
    {
        private readonly IServiceProvider _serviceProvider;

        // Child views
        public object QuotesView { get; }
        public object SalesOrdersView { get; }

        // Commands
        public ICommand NewQuoteCmd { get; }
        public ICommand NewSalesOrderCmd { get; }

        public SalesModuleViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Resolve child views
            QuotesView = _serviceProvider.GetRequiredService<Views.QuotesView>();
            SalesOrdersView = _serviceProvider.GetRequiredService<Views.DocumentsView>();  // Reuse DocumentsView, filtered to SALES_ORDER

            // Initialize commands
            NewQuoteCmd = new Commands.RelayCommand(async _ => await NewQuoteAsync());
            NewSalesOrderCmd = new Commands.RelayCommand(async _ => await NewSalesOrderAsync());
        }

        private async System.Threading.Tasks.Task NewQuoteAsync()
        {
            // Delegate to QuotesViewModel
            if (QuotesView is System.Windows.FrameworkElement fe && 
                fe.DataContext is ViewModels.QuotesViewModel quotesVm)
            {
                // Execute the command - it's async but wrapped in RelayCommand
                quotesVm.NewQuoteCommand.Execute(null);
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task NewSalesOrderAsync()
        {
            // R-062: Delegate to DocumentsViewModel (for SALES_ORDER)
            if (SalesOrdersView is System.Windows.FrameworkElement fe && 
                fe.DataContext is ViewModels.DocumentsViewModel docsVm)
            {
                // Execute NewSalesOrderCommand
                if (docsVm.NewSalesOrderCommand.CanExecute(null))
                {
                    docsVm.NewSalesOrderCommand.Execute(null);
                }
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        // IContextualActions implementation
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.NewCommand => NewQuoteCmd;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.ExportCommand => null;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.FiltersPreviewCommand => null;
    }
}


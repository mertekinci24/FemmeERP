// ReSharper disable once All
#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation
{
    public class ShellViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider _provider;

        // R-194 Phase 2: Direct Navigation Commands
        public ICommand NavigateDashboard { get; }
        public ICommand NavigateReports { get; }
        public ICommand MapsReports { get; }
        public ICommand MapsSalesOrders { get; }
        public ICommand MapsDispatches { get; }
        public ICommand MapsSalesInvoices { get; }
        public ICommand MapsPurchaseInvoices { get; }
        public ICommand MapsPartners { get; }
        public ICommand MapsCashBank { get; }
        public ICommand MapsStockCards { get; }
        public ICommand MapsWarehouses { get; }
        public ICommand MapsStockMovements { get; }
        public ICommand ToggleTheme { get; }
        public ICommand DiagnoseDbCmd { get; }
        // (legacy document shortcuts replaced by Maps*)

        // Contextual action commands (will be wired to the active view's actions)
        public ICommand NewCmd { get; private set; }
        public ICommand ExportCmd { get; private set; }
        public ICommand FiltersPreviewCmd { get; private set; }
        private readonly ICommand _disabledCmd;

        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set
            {
                if (!ReferenceEquals(_currentView, value))
                {
                    // Unsubscribe from old view's DataContextChanged
                    if (_currentView is System.Windows.FrameworkElement oldFe)
                    {
                        oldFe.DataContextChanged -= OnViewDataContextChanged;
                    }

                    _currentView = value;
                    OnPropertyChanged();

                    // Subscribe to new view's DataContextChanged for deferred binding
                    if (_currentView is System.Windows.FrameworkElement newFe)
                    {
                        newFe.DataContextChanged += OnViewDataContextChanged;
                    }

                    UpdateContextualActions(value);
                    _ = TryAutoRefreshAsync(value);
                }
            }
        }

        private void OnViewDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // When the view's DataContext changes (ViewModel attached), re-evaluate contextual actions
            UpdateContextualActions(_currentView);
            _ = TryAutoRefreshAsync(_currentView);
        }

        public ShellViewModel(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));

            // Direct maps (no module wrappers)
            NavigateDashboard = new RelayCommand(_ => CurrentView = _provider.GetRequiredService<Views.DashboardView>());
            NavigateReports = new RelayCommand(_ => CurrentView = _provider.GetRequiredService<Views.ReportsView>());
            MapsReports = NavigateReports;
            MapsSalesOrders = new RelayCommand(_ => ShowDocumentsFiltered("SALES_ORDER"));
            MapsDispatches = new RelayCommand(_ => ShowDocumentsFiltered("SEVK_IRSALIYESI"));
            MapsSalesInvoices = new RelayCommand(_ => ShowDocumentsFiltered("SALES_INVOICE"));
            MapsPurchaseInvoices = new RelayCommand(_ => ShowDocumentsFiltered("PURCHASE_INVOICE"));
            MapsPartners = new RelayCommand(_ => CurrentView = _provider.GetRequiredService<Views.PartnerListView>());
            MapsCashBank = new RelayCommand(_ => CurrentView = _provider.GetRequiredService<Views.CashAccountListView>());
            MapsStockCards = new RelayCommand(_ => CurrentView = _provider.GetRequiredService<Views.StocksView>());
            MapsWarehouses = new RelayCommand(_ => CurrentView = _provider.GetRequiredService<Views.WarehouseManagementView>());
            MapsStockMovements = new RelayCommand(_ => ShowDocumentsFiltered(null));

            ToggleTheme = new RelayCommand(_ => {/* Theme toggle logic (placeholder) */});

            // Action buttons: initially disabled until CurrentView provides contextual actions
            _disabledCmd = new DisabledCommand();
            NewCmd = _disabledCmd;
            ExportCmd = _disabledCmd;
            FiltersPreviewCmd = _disabledCmd;

            // Diagnostics command
            DiagnoseDbCmd = new RelayCommand(_ => ShowDbDiagnostics());

            // Global smart buttons delegate to active view's context
            GlobalNewCommand = new RelayCommand(_ => ExecuteContextualNew());
            GlobalExportCommand = new RelayCommand(_ => ExecuteContextualExport());

            // Default view will be set by the host on the UI thread after DI composition
        }

        private void ShowDocumentsFiltered(string? type)
        {
            var view = _provider.GetRequiredService<Views.DocumentsView>();
            // Forward filter to VM if available
            if (view.DataContext is ViewModels.DocumentsViewModel vm)
            {
                vm.ApplyNavigationType(type);
            }
            CurrentView = view;
        }

        private void ShowPartnersWindow()
        {
            // R-194.2: Embed PartnerListView in main content (UserControl)
            CurrentView = _provider.GetRequiredService<Views.PartnerListView>();
        }

        private void ShowDbDiagnostics()
        {
            // Create diagnostics dialog with its ViewModel and show modally
            var vm = new ViewModels.DbDiagnosticsViewModel(_provider);
            var dlg = new Views.DbDiagnosticsDialog { DataContext = vm };
            dlg.ShowDialog();
        }

        private void UpdateContextualActions(object? currentView)
        {
            InventoryERP.Presentation.Actions.IContextualActions? actions = null;
            if (currentView is InventoryERP.Presentation.Actions.IContextualActions ia)
            {
                actions = ia;
            }
            else if (currentView is System.Windows.FrameworkElement fe)
            {
                actions = fe.DataContext as InventoryERP.Presentation.Actions.IContextualActions;
            }

            NewCmd = actions?.NewCommand ?? _disabledCmd;
            ExportCmd = actions?.ExportCommand ?? _disabledCmd;
            FiltersPreviewCmd = actions?.FiltersPreviewCommand ?? _disabledCmd;

            OnPropertyChanged(nameof(NewCmd));
            OnPropertyChanged(nameof(ExportCmd));
            OnPropertyChanged(nameof(FiltersPreviewCmd));
        }

        // Global smart buttons
        public ICommand GlobalNewCommand { get; }
        public ICommand GlobalExportCommand { get; }

        private void ExecuteContextualNew()
        {
            var vm = ResolveContextualActions();
            var cmd = vm?.NewCommand;
            if (cmd != null && cmd.CanExecute(null)) cmd.Execute(null);
        }

        private void ExecuteContextualExport()
        {
            var vm = ResolveContextualActions();
            var cmd = vm?.ExportCommand;
            if (cmd != null && cmd.CanExecute(null)) cmd.Execute(null);
        }

        private InventoryERP.Presentation.Actions.IContextualActions? ResolveContextualActions()
        {
            if (CurrentView is InventoryERP.Presentation.Actions.IContextualActions ia) return ia;
            if (CurrentView is System.Windows.FrameworkElement fe) return fe.DataContext as InventoryERP.Presentation.Actions.IContextualActions;
            return null;
        }

        private async System.Threading.Tasks.Task TryAutoRefreshAsync(object? view)
        {
            try
            {
                if (view is InventoryERP.Presentation.Common.IAutoRefresh a1)
                {
                    await a1.RefreshAsync();
                    return;
                }
                if (view is System.Windows.FrameworkElement fe && fe.DataContext is InventoryERP.Presentation.Common.IAutoRefresh a2)
                {
                    await a2.RefreshAsync();
                }
            }
            catch { /* swallow refresh errors on navigation */ }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private EventHandler? _canExecuteChanged;
        public RelayCommand(Action<object?> execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add => _canExecuteChanged += value;
            remove => _canExecuteChanged -= value;
        }
        public void RaiseCanExecuteChanged() => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

internal class DisabledCommand : System.Windows.Input.ICommand
{
    public bool CanExecute(object? parameter) => false;
    public void Execute(object? parameter) { }
    public event System.EventHandler? CanExecuteChanged { add { } remove { } }
}


using InventoryERP.Application.Documents;
using InventoryERP.Application.Stocks;
using InventoryERP.Application.Products;
// ReSharper disable once All
#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows;
using InventoryERP.Presentation.Abstractions;
using InventoryERP.Presentation.ViewModels.Cash;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.Services
{
    /// <summary>
    /// R-037: WPF-specific implementation of IDialogService.
    /// Contains all WPF UI code (MessageBox, ShowDialog, etc.)
    /// </summary>
    public class WpfDialogService : IDialogService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly InventoryERP.Application.Documents.IDocumentQueries _docQueries;
        private readonly InventoryERP.Application.Stocks.IStockQueries _stockQueries;

        public WpfDialogService(
            IServiceScopeFactory scopeFactory,
           InventoryERP.Application.Documents.IDocumentQueries docQueries, InventoryERP.Application.Stocks.IStockQueries stockQueries)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _docQueries = docQueries ?? throw new ArgumentNullException(nameof(docQueries));
            _stockQueries = stockQueries ?? throw new ArgumentNullException(nameof(stockQueries));
        }

        public void ShowMessageBox(string message, string title = "Bilgi")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowStockInfo(string sku, string name, string baseUom, decimal onHandQty)
        {
            MessageBox.Show(
                $"Stok Bilgileri\n\nSKU: {sku}\nAd: {name}\nBirim: {baseUom}\nStok: {onHandQty:N2}",
                "Stok Bilgileri",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public async Task<bool> ShowAdjustmentDialogAsync(int documentId)
        {
            // Load DTO
            var dto = await _docQueries.GetAsync(documentId);
            if (dto is null) return false;

            IServiceScope? scope = null;
            try
            {
                scope = _scopeFactory.CreateScope();
                
                // R-072: Removed logger resolution (R-056 strategy abandoned in R-069)
                // R-069: Explicitly resolve IDialogService to ensure it's passed to ViewModel (required)
                var dialogService = scope.ServiceProvider.GetRequiredService<Abstractions.IDialogService>();
                // R-081: Do NOT pass null extras to ActivatorUtilities; null cannot be type-matched and
                // causes "suitable constructor" errors. Only pass the non-DI dto; let DI resolve the rest.
                var vm = ActivatorUtilities.CreateInstance<ViewModels.DocumentEditViewModel>(
                    scope.ServiceProvider,
                    dto); // Only DTO is passed explicitly; all other deps resolved from DI
                    
                var dlg = new Views.DocumentEditDialog(vm, scope);
                scope = null; // dialog owns scope

                var result = dlg.ShowDialog();
                return result == true;
            }
            finally
            {
                scope?.Dispose();
            }
        }

        public Task ShowStockMovementsAsync(int productId)
        {
            var dlg = new Views.StockMovesDialog(productId, _stockQueries);
            try
            {
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    dlg.Owner = app.MainWindow;
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore cross-thread access in edge cases
            }
            dlg.ShowDialog();
            return Task.CompletedTask;
        }

        public async Task<bool> ShowDocumentEditDialogAsync(int documentId)
        {
            // R-042: Generic document edit dialog - replaces ShowDialogSafe pattern
            var dto = await _docQueries.GetAsync(documentId);
            if (dto is null) return false;

            IServiceScope? scope = null;
            try
            {
                scope = _scopeFactory.CreateScope();
                // R-069: Explicitly resolve IDialogService to ensure it's passed to ViewModel (required)
                var dialogService = scope.ServiceProvider.GetRequiredService<Abstractions.IDialogService>();
                // R-072: Removed logger resolution (R-056 strategy abandoned in R-069)
                // R-081: Only pass DTO; DI provides services (IDocumentCommandService, IProductsReadService, IDialogService, IOptions<UiOptions>)
                var vm = ActivatorUtilities.CreateInstance<ViewModels.DocumentEditViewModel>(
                    scope.ServiceProvider,
                    dto);
                var dlg = new Views.DocumentEditDialog(vm, scope);
                scope = null; // dialog owns scope

                var result = dlg.ShowDialog();
                return result == true;
            }
            finally
            {
                scope?.Dispose();
            }
        }

        public Task<bool> ShowItemEditDialogAsync(int? productId)
        {
            using var scope = _scopeFactory.CreateScope();
            // Resolve dependencies for ItemEditViewModel from DI
            var db = scope.ServiceProvider.GetRequiredService<global::Persistence.AppDbContext>();
            var priceSvc = scope.ServiceProvider.GetRequiredService<InventoryERP.Application.Products.IPriceListService>();
            var dialogSvc = scope.ServiceProvider.GetRequiredService<Abstractions.IDialogService>();

            var vm = new ViewModels.ItemEditViewModel(db, priceSvc, dialogSvc, productId, Serilog.Log.Logger);
            var dlg = new Views.ItemEditDialog(vm);

            try
            {
                var app = System.Windows.Application.Current;
                if (app?.MainWindow != null)
                {
                    dlg.Owner = app.MainWindow;
                }
            }
            catch (InvalidOperationException)
            {
                // ignore owner assignment errors
            }

            return Task.FromResult(dlg.ShowDialog() == true);
        }

        public Task<bool> ShowCashReceiptDialogAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var vm = scope.ServiceProvider.GetRequiredService<CashReceiptDialogViewModel>();
            var dialog = new Views.CashReceiptDialog(vm);
            
            try
            {
                var app = System.Windows.Application.Current;
                if (app?.MainWindow != null)
                {
                    dialog.Owner = app.MainWindow;
                }
            }
            catch (InvalidOperationException)
            {
                // ignore owner assignment errors
            }

            return Task.FromResult(dialog.ShowDialog() == true);
        }

        public Task<bool> ShowCashPaymentDialogAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var vm = scope.ServiceProvider.GetRequiredService<CashPaymentDialogViewModel>();
            var dialog = new Views.CashPaymentDialog(vm);
            
            try
            {
                var app = System.Windows.Application.Current;
                if (app?.MainWindow != null)
                {
                    dialog.Owner = app.MainWindow;
                }
            }
            catch (InvalidOperationException)
            {
                // ignore owner assignment errors
            }

            return Task.FromResult(dialog.ShowDialog() == true);
        }

        public Task ShowErrorAsync(string title, string details)
        {
            MessageBox.Show(details, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return Task.CompletedTask;
        }
    }
}




// ReSharper disable once All
#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using InventoryERP.Application.Products;
using InventoryERP.Application.Stocks;
using InventoryERP.Application.Export;
using Microsoft.Win32;
using InventoryERP.Presentation.Commands;
using InventoryERP.Presentation.Abstractions;
using InventoryERP.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InventoryERP.Presentation.ViewModels
{
    // R-042: Stocks screen is for managing PRODUCT CARDS (master data), not transactions
    public sealed class StocksViewModel : ViewModelBase, InventoryERP.Presentation.Common.IAutoRefresh, InventoryERP.Presentation.Actions.IContextualActions
    {
        private readonly IProductsReadService _svc;
        private readonly IStockQueries _stockQueries;
        private readonly IStockExportService _stockExport;
        private readonly IExcelExportService _excelExportService;
        private readonly IDialogService _dialogService;
        private readonly InventoryERP.Application.Import.IImportService _importService;
        private readonly IPrintService _printService;

        public ObservableCollection<ProductRowDto> Rows { get; } = new();
        public RelayCommand RefreshCmd { get; }
        public RelayCommand ShowMovementsCmd { get; }
        public RelayCommand ExportExcelCmd { get; }
        public RelayCommand NewStockCmd { get; }
        public RelayCommand EditItemCmd { get; } // R-043: Edit existing product
        public RelayCommand ImportCsvCmd { get; } // R-040: CSV Import
        public RelayCommand DeleteStockCmd { get; }
        public RelayCommand ClearSearchCmd { get; } // R-040: Clear search
        // R-048: Right-click menu commands
        public RelayCommand ShowStockInfoCmd { get; }
        public RelayCommand CreateAdjustmentDocumentCmd { get; }
        public RelayCommand PrintBarcodeCmd { get; }

        public ProductRowDto? Selected { get => _selected; set => SetProperty(ref _selected, value); }
        private ProductRowDto? _selected;

        // R-040: Fast search
        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = SearchAsync(); // Fire and forget search on text change
                }
            }
        }

        private readonly InventoryERP.Application.Documents.IDocumentCommandService _docCmdSvc;
        private readonly IServiceProvider _serviceProvider;

        // R-210: Warehouse Filter
        public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
        private WarehouseDto? _selectedWarehouseFilter;
        public WarehouseDto? SelectedWarehouseFilter
        {
            get => _selectedWarehouseFilter;
            set
            {
                if (SetProperty(ref _selectedWarehouseFilter, value))
                {
                    _ = RefreshAsync();
                }
            }
        }

        public StocksViewModel(
            IProductsReadService svc,
            IStockQueries stockQueries,
            IStockExportService stockExport,
            IExcelExportService excelExportService,
            InventoryERP.Application.Documents.IDocumentCommandService docCmdSvc,
            IDialogService dialogService,
            InventoryERP.Application.Import.IImportService importService,
            IPrintService printService,
            IServiceProvider serviceProvider)
        {
            _svc = svc;
            _stockQueries = stockQueries;
            _stockExport = stockExport;
            _excelExportService = excelExportService;
            _docCmdSvc = docCmdSvc;
            _dialogService = dialogService;
            _importService = importService;
            _printService = printService;
            _serviceProvider = serviceProvider;

            RefreshCmd = new RelayCommand(async _ => await RefreshAsync());
            ShowMovementsCmd = new RelayCommand(async param => await ShowMovementsAsync(param as ProductRowDto));
            ExportExcelCmd = new RelayCommand(async param => await ExportExcelAsync(param));
            NewStockCmd = new RelayCommand(async _ => await NewStockAsync()); // R-040: Open ItemEditDialog
            EditItemCmd = new RelayCommand(async param => await EditItemAsync(param as ProductRowDto)); // R-043: Edit existing product
            ShowStockInfoCmd = new RelayCommand(param => ShowStockInfo(param as ProductRowDto));
            CreateAdjustmentDocumentCmd = new RelayCommand(async param => await CreateAdjustmentDocumentAsync(param as ProductRowDto));
            DeleteStockCmd = new RelayCommand(async param => await DeleteStockAsync(param as ProductRowDto));
            PrintBarcodeCmd = new RelayCommand(param => PrintBarcode(param as ProductRowDto));
            ImportCsvCmd = new RelayCommand(async _ => await ImportCsvAsync()); // R-040
            ClearSearchCmd = new RelayCommand(_ => SearchText = ""); // R-040
            
            // Load Warehouses for Filter
            _ = LoadWarehousesAsync();
        }

        private async Task LoadWarehousesAsync()
        {
            try
            {
                var warehouses = await _svc.GetWarehousesAsync();
                Warehouses.Clear();
                // Add "All" option
                Warehouses.Add(new WarehouseDto(0, "TÜM DEPOLAR", false));
                foreach (var w in warehouses) Warehouses.Add(w);
                
                SelectedWarehouseFilter = Warehouses.FirstOrDefault();
            }
            catch { /* Ignore errors during load */ }
        }

        private void ShowStockInfo(ProductRowDto? product)
        {
            if (product is null)
            {
                _dialogService.ShowMessageBox("Ürün seçilmedi.");
                return;
            }

            _dialogService.ShowStockInfo(product.Sku, product.Name, product.BaseUom, product.OnHandQty);
        }

        // ... (CreateAdjustmentDocumentAsync omitted for brevity, no changes needed there) ...
        
        // ... (ShowMovementsAsync, ExportExcelAsync omitted) ...

        // R-282: Passive Lifecycle Management
        private bool _showInactive;
        public bool ShowInactive
        {
            get => _showInactive;
            set
            {
                if (SetProperty(ref _showInactive, value))
                {
                    _ = RefreshAsync();
                }
            }
        }

        public async Task RefreshAsync()
        {
            Rows.Clear();
            // R-210: Pass Warehouse Filter (0 means All, so pass null if Id is 0)
            int? warehouseId = (SelectedWarehouseFilter?.Id ?? 0) == 0 ? null : SelectedWarehouseFilter?.Id;
            // R-282: Pass ShowInactive flag
            foreach (var x in await _svc.GetListAsync(SearchText, warehouseId, ShowInactive)) Rows.Add(x);
        }

        private async Task SearchAsync()
        {
            Rows.Clear();
            int? warehouseId = (SelectedWarehouseFilter?.Id ?? 0) == 0 ? null : SelectedWarehouseFilter?.Id;
            var results = await _svc.GetListAsync(string.IsNullOrWhiteSpace(SearchText) ? null : SearchText, warehouseId);
            foreach (var x in results) Rows.Add(x);
        }

        // R-326: Safe Import Mode (Default: False = Safe)
        private bool _importUpdateExisting;
        public bool ImportUpdateExisting
        {
            get => _importUpdateExisting;
            set => SetProperty(ref _importUpdateExisting, value);
        }

        private async Task ImportCsvAsync()
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "CSV dosyaları (*.csv)|*.csv|Tüm dosyalar (*.*)|*.*",
                    Title = "Ürün listesi içeren CSV dosyasını seçin"
                };

                if (ofd.ShowDialog() == true)
                {
                    // R-326: Pass safeMode inverse of UpdateExisting
                    var result = await _importService.ImportProductsFromCsvAsync(ofd.FileName, safeMode: !ImportUpdateExisting);
                    
                    if (result.Skipped > 0)
                    {
                        var msg = $"İçe aktarma tamamlandı.\n\n" +
                                  $"✅ Eklendi: {result.Added}\n" +
                                  $"🔄 Güncellendi: {result.Updated}\n" +
                                  $"⏭️ Atlandı (Mevcut): {result.Skipped}\n\n" +
                                  $"Toplam: {result.Total}";
                        _dialogService.ShowMessageBox(msg, "İçe Aktarma Sonucu");
                    }
                    else
                    {
                         _dialogService.ShowMessageBox($"{result.Added} ürün başarıyla eklendi, {result.Updated} güncellendi.", "Başarılı");
                    }
                    
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBox($"CSV içe aktarma sırasında hata oluştu: {ex.Message}", "Hata");
            }
        }

        private async Task NewStockAsync()
        {
            try
            {
                if (await _dialogService.ShowItemEditDialogAsync(null))
                {
                    await RefreshAsync();
                    _dialogService.ShowMessageBox("Yeni stok kartı başarıyla oluşturuldu.", "Başarılı");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBox($"Stok kartı oluşturulamadı: {ex.Message}", "Hata");
            }
        }

        private async Task EditItemAsync(ProductRowDto? product)
        {
            if (product is null)
            {
                _dialogService.ShowMessageBox("Ürün seçilmedi.", "Uyarı");
                return;
            }

            try
            {
                if (await _dialogService.ShowItemEditDialogAsync(product.Id))
                {
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBox($"Stok kartı düzenlenemedi: {ex.Message}", "Hata");
            }
        }

        private async Task DeleteStockAsync(ProductRowDto? product)
        {
            if (product is null)
            {
                _dialogService.ShowMessageBox("Ürün seçilmedi.", "Uyarı");
                return;
            }
            var res = System.Windows.MessageBox.Show($"{product.Name} silinsin mi?", "Onay", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (res != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                await mediator.Send(new InventoryERP.Infrastructure.CQRS.Commands.DeleteProductCommand(product.Id));
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBox($"Hata: {ex.Message}", "Silme İşlemi Başarısız");
            }
        }

        private void PrintBarcode(ProductRowDto? product)
        {
            var item = product ?? Selected;
            if (item is null)
            {
                _dialogService.ShowMessageBox("Ürün seçilmedi.", "Uyarı");
                return;
            }

            // Placeholder: delegate to print service for actual label generation
            _ = _printService.GenerateProductLabelAsync(item);
            _dialogService.ShowMessageBox($"Etiket yazdırılıyor: {item.Name}", "Yer tutucu");
        }

        System.Windows.Input.ICommand? InventoryERP.Presentation.Actions.IContextualActions.NewCommand => NewStockCmd;
        System.Windows.Input.ICommand? InventoryERP.Presentation.Actions.IContextualActions.ExportCommand => ExportExcelCmd;
        System.Windows.Input.ICommand? InventoryERP.Presentation.Actions.IContextualActions.FiltersPreviewCommand => null;
        private async Task ShowMovementsAsync(ProductRowDto? product)
    {
        if (product is null) return;
        try
        {
            await _dialogService.ShowStockMovementsAsync(product.Id);
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessageBox($"Stok hareketleri açılamadı: {ex.Message}", "Hata");
        }
    }

    private async Task ExportExcelAsync(object? param)
    {
        try
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FileName = $"Stok_Listesi_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                await Task.Run(() => _excelExportService.ExportToExcel(Rows, sfd.FileName, "Stoklar"));
                _dialogService.ShowMessageBox("Excel dışa aktarma tamamlandı.", "Başarılı");
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { }
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessageBox($"Excel dışa aktarma hatası: {ex.Message}", "Hata");
        }
    }

    private async Task CreateAdjustmentDocumentAsync(ProductRowDto? product)
    {
        try
        {
            var dto = new InventoryERP.Application.Documents.DTOs.DocumentDetailDto
            {
                Type = InventoryERP.Domain.Enums.DocumentType.ADJUSTMENT_IN.ToString(),
                Date = DateTime.Today
            };

            var id = await _docCmdSvc.CreateDraftAsync(dto);

            // If product is selected, we could potentially add it as a line here, 
            // but for now we just open the empty document.
            
            if (await _dialogService.ShowDocumentEditDialogAsync(id))
            {
                await RefreshAsync();
            }
            else
            {
                // If cancelled, we might want to delete the draft, 
                // but usually drafts are kept until explicitly deleted or cleaned up.
                // For now, we leave it as is.
                await _docCmdSvc.DeleteDraftAsync(id);
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessageBox($"Düzeltme fişi oluşturulamadı: {ex.Message}", "Hata");
        }
    }
}
}

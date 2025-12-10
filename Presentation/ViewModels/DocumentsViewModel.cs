
// ReSharper disable once All
#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using Microsoft.Extensions.DependencyInjection;
using InventoryERP.Presentation.Commands;
using InventoryERP.Presentation.Views; // R-038: CashReceiptDialog
using System.Diagnostics;
using System.IO;

namespace InventoryERP.Presentation.ViewModels
{
    public sealed class DocumentsViewModel : ViewModelBase, InventoryERP.Presentation.Actions.IContextualActions
    {
    private readonly IDocumentQueries _queries;
    private readonly IInvoiceCommandService _svc;
    private readonly InventoryERP.Application.Documents.IDocumentCommandService _cmdSvc;
    private readonly InventoryERP.Application.Products.IProductsReadService _productsSvc;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly Serilog.ILogger _logger; // R-037: Diagnostic logging
    private readonly Abstractions.IDialogService _dialogService; // R-042: Dialog abstraction

        public ObservableCollection<DocumentRowDto> Rows { get; } = new();
        public RelayCommand RefreshCmd { get; }
    public RelayCommand NewSalesInvCmd { get; }
    private RelayCommand? _newPurchaseInvCmd;
        public RelayCommand NewSalesOrderCmd { get; } // R-062: Sales Order
        public RelayCommand ApproveCmd { get; }
        public RelayCommand CancelCmd { get; }
    public RelayCommand EditCmd { get; }
    public RelayCommand DeleteCmd { get; }
        public RelayCommand PrintCmd { get; }
        public RelayCommand ExportCmd { get; }
    public RelayCommand LandedCostCmd { get; }
    public RelayCommand ReportTestCmd { get; }
    public RelayCommand ImportCsvCmd { get; }
    public RelayCommand ImportOpeningStockCmd { get; } // R-033.A: Opening Stock Import
    public RelayCommand SendEInvoiceCmd { get; }
    public RelayCommand ConvertToDispatchCmd { get; }
    public RelayCommand ConvertToInvoiceCmd { get; }
    // R-042/R-043: Document-Centric Architecture - all transactions in Documents menu
    public RelayCommand NewCashReceiptCmd { get; }
    public RelayCommand NewTransferCmd { get; }
    public RelayCommand NextPageCmd { get; }
        public RelayCommand PrevPageCmd { get; }
        public RelayCommand QuickTodayCmd { get; }
        public RelayCommand Quick7Cmd { get; }
        public RelayCommand Quick30Cmd { get; }
    // aliases for XAML compatibility
    public RelayCommand RefreshCommand => RefreshCmd;
    public RelayCommand NewInvoiceCommand => NewSalesInvCmd;
    public RelayCommand NewSalesOrderCommand => NewSalesOrderCmd; // R-062
    public RelayCommand NewCashReceiptCommand => NewCashReceiptCmd;
    public RelayCommand NewTransferCommand => NewTransferCmd;
    public RelayCommand ApproveCommand => ApproveCmd;
    public RelayCommand CancelCommand => CancelCmd;
    public RelayCommand EditCommand => EditCmd;
    public RelayCommand PrintCommand => PrintCmd;
    public RelayCommand ExportCommand => ExportCmd;

        private string? _searchText;
        public string? SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) _ = RefreshAsync(); } }
        private System.DateTime? _dateFrom;
        public System.DateTime? DateFrom { get => _dateFrom; set { if (SetProperty(ref _dateFrom, value)) _ = RefreshAsync(); } }
        private System.DateTime? _dateTo;
        public System.DateTime? DateTo { get => _dateTo; set { if (SetProperty(ref _dateTo, value)) _ = RefreshAsync(); } }
        public int RowCount => Rows.Count;
    private DocumentRowDto? _selected;
    public DocumentRowDto? Selected { get => _selected; set { if (SetProperty(ref _selected, value)) { OnPropertyChanged(nameof(CanConvertSelected)); OnPropertyChanged(nameof(CanConvertToInvoiceSelected)); } } }

        public bool CanConvertSelected => Selected is not null
            && string.Equals(Selected.Type, "SALES_ORDER", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(Selected.Status, "APPROVED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Selected.Status, "POSTED", StringComparison.OrdinalIgnoreCase));

        public bool CanConvertToInvoiceSelected => Selected is not null
            && string.Equals(Selected.Type, "SEVK_IRSALIYESI", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(Selected.Status, "APPROVED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Selected.Status, "POSTED", StringComparison.OrdinalIgnoreCase));

    // Paging
    private int _page = 1;
    public int Page { get => _page; set { if (SetProperty(ref _page, value)) _ = RefreshAsync(); } }
    private int _pageSize = 20;
    public int PageSize { get => _pageSize; set { if (SetProperty(ref _pageSize, value)) { Page = 1; _ = RefreshAsync(); OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(HasNext)); OnPropertyChanged(nameof(HasPrev)); } } }
    private int _totalCount;
    public int TotalCount { get => _totalCount; set { if (SetProperty(ref _totalCount, value)) { OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(HasNext)); OnPropertyChanged(nameof(HasPrev)); } } }
    public bool HasNext => Page * PageSize < TotalCount;
    public bool HasPrev => Page > 1;
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

        public DocumentsViewModel(IDocumentQueries queries, IInvoiceCommandService svc,InventoryERP.Application.Documents.IDocumentCommandService cmdSvc,InventoryERP.Application.Products.IProductsReadService productsSvc, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory, IServiceProvider serviceProvider, Serilog.ILogger logger, Abstractions.IDialogService dialogService)
        {
            _queries = queries;
            _svc = svc;
            _cmdSvc = cmdSvc;
            _productsSvc = productsSvc;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _logger = logger; // R-037
            _dialogService = dialogService; // R-042
            RefreshCmd     = new RelayCommand(async _ => await RefreshAsync());
            NewSalesInvCmd = new RelayCommand(async _ => await NewSalesInvoiceAsync());
            NewSalesOrderCmd = new RelayCommand(async _ => await NewSalesOrderAsync()); // R-062
            ApproveCmd     = new RelayCommand(async _ => await ApproveSelectedAsync());
            CancelCmd      = new RelayCommand(async _ => await CancelSelectedAsync());
            ConvertToDispatchCmd = new RelayCommand(async _ => await ConvertSelectedToDispatchAsync());
            ConvertToInvoiceCmd = new RelayCommand(async _ => await ConvertSelectedToInvoiceAsync());
            EditCmd        = new RelayCommand(async _ => await EditSelectedAsync());
            DeleteCmd      = new RelayCommand(async _ => await DeleteSelectedAsync());
            PrintCmd       = new RelayCommand(async _ => await PrintSelectedAsync());
            ExportCmd      = new RelayCommand(async _ => await ExportWithUiAsync());
            LandedCostCmd  = new RelayCommand(_ => OpenLandedCostDialog());
            ReportTestCmd  = new RelayCommand(async _ => await GenerateTestReportAsync());
            ImportCsvCmd   = new RelayCommand(async _ => await ImportProductsCsvAsync());
            ImportOpeningStockCmd = new RelayCommand(async _ => await ImportOpeningStockCsvAsync()); // R-033.A
            SendEInvoiceCmd= new RelayCommand(async _ => await SendSelectedEInvoiceAsync());
            // R-042/R-043: Document-Centric Architecture - all transactions in Documents menu
            NewCashReceiptCmd = new RelayCommand(async _ => await OpenCashReceiptDialogAsync());
            NewTransferCmd = new RelayCommand(async _ => await NewTransferDocumentAsync());
            CopyFiltersCmd = new RelayCommand(_ => CopyFiltersToClipboard());
            NextPageCmd    = new RelayCommand(_ => { if (HasNext) Page++; });
            PrevPageCmd    = new RelayCommand(_ => { if (HasPrev) Page--; });
            QuickTodayCmd  = new RelayCommand(_ => { DateFrom = DateTime.Today; DateTo = DateTime.Today; _ = RefreshAsync(); });
            Quick7Cmd      = new RelayCommand(_ => { DateFrom = DateTime.Today.AddDays(-7); DateTo = DateTime.Today; _ = RefreshAsync(); });
            Quick30Cmd     = new RelayCommand(_ => { DateFrom = DateTime.Today.AddDays(-30); DateTo = DateTime.Today; _ = RefreshAsync(); });
        }

        public RelayCommand ConvertToDispatchCommand => ConvertToDispatchCmd;
    public RelayCommand ConvertToInvoiceCommand => ConvertToInvoiceCmd;

    public async Task ConvertSelectedToDispatchAsync()
        {
            if (Selected is null) return;
            if (!string.Equals(Selected.Type, "SALES_ORDER", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.MessageBox.Show("Selected document is not a SALES_ORDER", "Cannot convert", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            // R-262: Allow both APPROVED and POSTED status for conversion
            if (!string.Equals(Selected.Status, "APPROVED", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Selected.Status, "POSTED", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.MessageBox.Show("Sales order must be approved before conversion.", "Cannot convert", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var newId = await _cmdSvc.ConvertSalesOrderToDispatchAsync(Selected.Id);
            await RefreshAsync();
            Selected = Rows.FirstOrDefault(r => r.Id == newId);
        }

        public async Task ConvertSelectedToInvoiceAsync()
        {
            if (Selected is null) return;
            if (!string.Equals(Selected.Type, "SEVK_IRSALIYESI", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.MessageBox.Show("Selected document is not a SEVK_IRSALIYESI", "Cannot convert", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (!(string.Equals(Selected.Status, "APPROVED", StringComparison.OrdinalIgnoreCase) || string.Equals(Selected.Status, "POSTED", StringComparison.OrdinalIgnoreCase)))
            {
                System.Windows.MessageBox.Show("Dispatch must be approved before conversion.", "Cannot convert", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var newId = await _cmdSvc.ConvertDispatchToInvoiceAsync(Selected.Id);
            await RefreshAsync();
            Selected = Rows.FirstOrDefault(r => r.Id == newId);
        }

        public RelayCommand CopyFiltersCmd { get; }

        public string ActiveFiltersPreview => BuildFilterSummary();

        private string BuildFilterSummary()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (DateFrom is not null || DateTo is not null)
            {
                var from = DateFrom?.ToString("yyyy-MM-dd") ?? "";
                var to = DateTo?.ToString("yyyy-MM-dd") ?? "";
                parts.Add($"Date: {from} - {to}");
            }
            if (!string.IsNullOrWhiteSpace(SearchText)) parts.Add($"Search: {SearchText}");
            return parts.Count == 0 ? "(yok)" : string.Join("; ", parts);
        }

        private void CopyFiltersToClipboard()
        {
            try
            {
                System.Windows.Clipboard.SetText(ActiveFiltersPreview ?? string.Empty);
            }
            catch { /* ignore clipboard failures */ }
        }

        private async Task ExportWithUiAsync()
        {
            var filter = new InventoryERP.Application.Documents.DTOs.DocumentListFilter
            {
                SearchText = SearchText,
                DateFrom = DateFrom,
                DateTo = DateTo,
                Type = CurrentType,
                Page = Page,
                PageSize = PageSize
            };

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<InventoryERP.Application.Documents.IDocumentReportService>();

                // build default filename
                var typeSafe = string.IsNullOrWhiteSpace(CurrentType) ? "ALL" : CurrentType!;
                var dateSafe = (DateFrom ?? DateTime.Today).ToString("yyyyMMdd");
                var defaultName = $"Inventory_{typeSafe}_{dateSafe}.xlsx";

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (.xlsx)|*.xlsx",
                    FileName = defaultName,
                    Title = "Dışa aktarılacak dosyayı seçin"
                };
                var ok = dlg.ShowDialog();
                if (ok != true) return;

                var excelBytes = await svc.ExportListExcelAsync(filter);
                await System.IO.File.WriteAllBytesAsync(dlg.FileName, excelBytes);
                System.Windows.MessageBox.Show("Dışa aktarma tamamlandı.", "Başarılı", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); } catch { }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Export error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool TryWriteFileWithRetries(string path, byte[] bytes, int maxAttempts = 3)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    System.IO.File.WriteAllBytes(path, bytes);
                    return true;
                }
                catch (System.IO.IOException) { System.Threading.Thread.Sleep(200); }
                catch (UnauthorizedAccessException) { System.Threading.Thread.Sleep(200); }
            }
            return false;
        }

        private string? _currentType; // R-194: Navigation filter
        public string? CurrentType
        {
            get => _currentType;
            private set
            {
                if (SetProperty(ref _currentType, value))
                {
                    OnPropertyChanged(nameof(DisplayTitle));
                    // Reset paging when type changes
                    Page = 1;
                }
            }
        }

        public string DisplayTitle
        {
            get
            {
                return CurrentType switch
                {
                    "SALES_ORDER" => "Satış Siparişleri",
                    "SEVK_IRSALIYESI" => "Sevk İrsaliyeleri",
                    "SALES_INVOICE" => "Satış Faturaları",
                    "PURCHASE_INVOICE" => "Alış Faturaları",
                    _ => "Belgeler"
                };
            }
        }

        // R-194: Entry point for Shell navigation
        public async void ApplyNavigationType(string type)
        {
            // Step 1: visually clear
            Rows.Clear();
            // Step 2: set type
            CurrentType = type;
            // Step 3: force reload
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            Rows.Clear();
            var filter = new InventoryERP.Application.Documents.DTOs.DocumentListFilter
            {
                SearchText = SearchText,
                DateFrom = DateFrom,
                DateTo = DateTo,
                Type = CurrentType,
                Page = Page,
                PageSize = PageSize
            };

            var res = await _queries.ListAsync(filter, Page, PageSize);
            foreach (var x in res.Items) Rows.Add(x);
            TotalCount = res.TotalCount;
            OnPropertyChanged(nameof(RowCount));
            OnPropertyChanged(nameof(HasNext));
            OnPropertyChanged(nameof(HasPrev));
        }

        // R-045: Public methods for module delegation
        public async Task ExecuteNewInvoiceAsync() => await NewSalesInvoiceAsync();
        public async Task ExecuteNewCashReceiptAsync() => await OpenCashReceiptDialogAsync();
        public async Task ExecuteNewTransferAsync() => await NewTransferDocumentAsync();

        private async Task NewSalesInvoiceAsync()
        {
            var id = await _svc.CreateDraftAsync(new CreateInvoiceDraftDto("SALES_INVOICE", System.DateTime.Today));
            await RefreshAsync();
            Selected = Rows.FirstOrDefault(r => r.Id == id);
            
            // R-042: Use IDialogService instead of ShowDialogSafe
            var result = await _dialogService.ShowDocumentEditDialogAsync(id);
            if (result)
            {
                await RefreshAsync();
            }
            else
            {
                // R-194.6: User cancelled — remove ghost draft for SALES_INVOICE
                try { await _cmdSvc.DeleteDraftAsync(id); } catch { }
                await RefreshAsync();
            }
        }

        // Create new Purchase Invoice draft (R-194.1)
        private async Task NewPurchaseInvoiceAsync()
        {
            var id = await _svc.CreateDraftAsync(new CreateInvoiceDraftDto("PURCHASE_INVOICE", System.DateTime.Today));
            await RefreshAsync();
            Selected = Rows.FirstOrDefault(r => r.Id == id);

            var result = await _dialogService.ShowDocumentEditDialogAsync(id);
            if (result)
            {
                await RefreshAsync();
            }
            else
            {
                // R-194.2: User cancelled — remove ghost draft
                await _cmdSvc.DeleteDraftAsync(id);
                await RefreshAsync();
            }
        }

        // R-062: Create new Sales Order draft
        private async Task NewSalesOrderAsync()
        {
            var id = await _svc.CreateDraftAsync(new CreateInvoiceDraftDto("SALES_ORDER", System.DateTime.Today));
            await RefreshAsync();
            Selected = Rows.FirstOrDefault(r => r.Id == id);
            
            // R-042: Use IDialogService instead of ShowDialogSafe
            var result = await _dialogService.ShowDocumentEditDialogAsync(id);
            if (result)
            {
                await RefreshAsync();
            }
        }

        // Dialog aÃ§ma mantÄ±ÄŸÄ± (Ã¶rnek)
        private async Task EditSelectedAsync()
        {
            if (Selected is null) return;
            
            // R-042: Use IDialogService instead of ShowDialogSafe
            var result = await _dialogService.ShowDocumentEditDialogAsync(Selected.Id);
            if (result)
            {
                await RefreshAsync();
            }
        }

        private async Task DeleteSelectedAsync()
        {
            if (Selected is null) return;
            
            // R-243: Validate DRAFT status before attempting delete
            if (Selected.Status != "DRAFT")
            {
                System.Windows.MessageBox.Show("Sadece DRAFT durumundaki belgeler silinebilir.", "Uyarı", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            var res = System.Windows.MessageBox.Show($"Delete document {Selected.Number}?", "Confirm", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (res == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    await _cmdSvc.DeleteDraftAsync(Selected.Id);
                    await RefreshAsync();
                    System.Windows.MessageBox.Show("Belge başarıyla silindi.", "Bilgi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (InvalidOperationException ex) when (ex.Message == "DOC-404")
                {
                    System.Windows.MessageBox.Show("Belge bulunamadı - zaten silinmiş olabilir.", "Uyarı", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    await RefreshAsync();
                }
                catch (InvalidOperationException ex) when (ex.Message == "DOC-NOT-DRAFT")
                {
                    System.Windows.MessageBox.Show("Sadece DRAFT durumundaki belgeler silinebilir.", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Silme hatası: {ex.Message}", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

                        private async Task PrintSelectedAsync()
        {
            if (Selected is null)
            {
                System.Windows.MessageBox.Show("Belge seçilmedi.", "Uyarı", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var printSvc = scope.ServiceProvider.GetRequiredService<InventoryERP.Infrastructure.Services.IPrintService>();
                var docDetail = await _queries.GetAsync(Selected.Id);
                if (docDetail is null)
                {
                    System.Windows.MessageBox.Show("Belge detayları alınamadı.", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"FATURA_{Selected.Number}_{System.DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (sfd.ShowDialog() != true)
                    return;

                var bytes = await printSvc.GenerateInvoicePdfAsync(docDetail);
                await System.IO.File.WriteAllBytesAsync(sfd.FileName, bytes);
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                }
                catch { }
                System.Windows.MessageBox.Show($"Fatura PDF oluşturuldu:\n{sfd.FileName}", "Bilgi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Yazdırma hatası: {ex.Message}", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task GenerateTestReportAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<InventoryERP.Application.Reports.IReportingService>();
                var bytes = await svc.GenerateAsync("Test Raporu", "Bu bir test PDF iÃ§eriÄŸidir.");
                System.Windows.MessageBox.Show(bytes != null && bytes.Length > 100 ? "PDF Ã¼retildi." : "PDF oluÅŸturulamadÄ±.", "Rapor", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Reporting failed: {ex.Message}", "Rapor HatasÄ±", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void OpenLandedCostDialog()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<global::InventoryERP.Infrastructure.Services.ILandedCostService>();
                var dlg = new Views.LandedCostAllocationDialog(_queries, svc);
                dlg.Owner = System.Windows.Application.Current?.MainWindow;
                dlg.ShowDialog();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Landed cost dialog failed: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExportExcel()
        {
            // TODO: Excel export
        }

        private async Task ImportProductsCsvAsync()
        {
            try
            {
                _logger.Information("R-037: CSV Import iÅŸlemi baÅŸlatÄ±ldÄ± (Product Master Import)");
                
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

        private async Task ImportOpeningStockCsvAsync() // R-033.A: Opening Stock Import
        {
            try
            {
                _logger.Information("R-037: Opening Stock Import iÅŸlemi baÅŸlatÄ±ldÄ±");
                
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

        private async Task SendSelectedEInvoiceAsync()
        {
            if (Selected is null) return;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var adapter = scope.ServiceProvider.GetRequiredService<InventoryERP.Application.EInvoicing.IEInvoiceAdapter>();
                await adapter.SendInvoiceAsync(Selected.Id);
                await RefreshAsync();
                System.Windows.MessageBox.Show("E-fatura gÃ¶nderildi (mock)", "E-Fatura", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"E-fatura gÃ¶nderimi baÅŸarÄ±sÄ±z: {ex.Message}", "E-Fatura", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // R-042/R-043: Create new cash receipt document
        private async Task OpenCashReceiptDialogAsync()
        {
            try
            {
                _logger.Information("R-217: Opening Cash Receipt dialog (financial dialog, not stock)");
                
                // R-217: Use dedicated financial dialog instead of stock document dialog
                var result = await _dialogService.ShowCashReceiptDialogAsync();
                if (result)
                {
                    _logger.Information("R-217: Cash receipt saved successfully");
                    await RefreshAsync();
                    Messaging.EventBus.RaiseCashTransactionSaved(); // R-219: Notify CashAccountsViewModel to refresh
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "R-042: Error creating cash receipt");
                System.Windows.MessageBox.Show($"Tahsilat fiÅŸi oluÅŸturulamadÄ±: {ex.Message}", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // R-042/R-043: Create new transfer document
        private async Task NewTransferDocumentAsync()
        {
            try
            {
                _logger.Information("R-042: Creating new transfer document from DocumentsView");
                
                // Create draft transfer document
                var id = await _cmdSvc.CreateDraftAsync(new InventoryERP.Application.Documents.DTOs.DocumentDetailDto
                {
                    Type = "TRANSFER_FISI",
                    Number = $"TRF-{DateTime.Now:yyyyMMddHHmmss}",
                    Date = DateTime.Today,
                    Currency = "TRY",
                    Lines = new System.Collections.Generic.List<InventoryERP.Application.Documents.DTOs.DocumentLineDto>()
                });
                
                await RefreshAsync();
                
                // R-042: Use IDialogService instead of ShowDialogSafe
                var result = await _dialogService.ShowDocumentEditDialogAsync(id);
                if (result)
                {
                    _logger.Information("R-042: Transfer document saved successfully");
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "R-042: Error creating transfer document");
                System.Windows.MessageBox.Show($"Transfer fiÅŸi oluÅŸturulamadÄ±: {ex.Message}", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // R-042: ShowDialogSafe method removed - now using IDialogService abstraction
        // This eliminates the need for INVENTORYERP_TEST_MODE environment variable hack

        private async Task ApproveSelectedAsync()
        {
            if (Selected is null) return;
            // R-262: Save ID before refresh to restore selection
            var savedId = Selected.Id;
            await _svc.ApproveAsync(new ApproveInvoiceDto(Selected.Id));
            await RefreshAsync();
            // R-262: Restore selection to updated row so UI reflects new status
            if (savedId > 0)
            {
                Selected = Rows.FirstOrDefault(r => r.Id == savedId);
            }
        }

        private async Task CancelSelectedAsync()
        {
            if (Selected is null) return;
            await _svc.CancelAsync(new CancelInvoiceDto(Selected.Id));
            await RefreshAsync();
        }

        // IContextualActions implementation (explicit to avoid naming conflicts)
        System.Windows.Input.ICommand? InventoryERP.Presentation.Actions.IContextualActions.NewCommand =>
            CurrentType switch
            {
                "SALES_ORDER" => NewSalesOrderCmd,
                "SALES_INVOICE" => NewSalesInvCmd,
                "PURCHASE_INVOICE" => (_newPurchaseInvCmd ??= new RelayCommand(async _ => await NewPurchaseInvoiceAsync())),
                _ => NewSalesInvCmd // fallback
            };
        System.Windows.Input.ICommand? InventoryERP.Presentation.Actions.IContextualActions.ExportCommand => ExportCmd;
        System.Windows.Input.ICommand? InventoryERP.Presentation.Actions.IContextualActions.FiltersPreviewCommand => CopyFiltersCmd;
    }
}








// ReSharper disable once All
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Export;
using InventoryERP.Application.Import;
using InventoryERP.Application.Partners;
using InventoryERP.Domain.Enums;
using InventoryERP.Presentation.Commands;

namespace InventoryERP.Presentation.ViewModels;

/// <summary>
/// R-086: Partner (Cari) List ViewModel
/// R-095: Added list export to Excel and PDF
/// </summary>
public sealed partial class PartnerListViewModel : ViewModelBase
{
    private const int LedgerPageSize = 250;
    private readonly IPartnerService _partnerService;
    private readonly IPartnerReadService _partnerReadService;
    private readonly Abstractions.IDialogService _dialogService;
    private readonly IExcelImportService _excelImportService;
    private readonly Abstractions.IFileDialogService _fileDialogService;
    private readonly IExcelExportService _excelExportService;
    private readonly IListPdfExportService _listPdfExportService;
    private readonly IPartnerExportService _partnerExportService;
    
    public ObservableCollection<PartnerRowDto> Partners { get; } = new();
    
    public Commands.RelayCommand RefreshCommand { get; }
    public Commands.RelayCommand NewCommand { get; }
    public Commands.RelayCommand EditCommand { get; }
    public Commands.RelayCommand DeleteCommand { get; }
    public Commands.RelayCommand ImportFromExcelCommand { get; }
    public Commands.RelayCommand ExportListToExcelCommand { get; }
    public Commands.RelayCommand ExportListToPdfCommand { get; }
    public Commands.RelayCommand OpenStatementCommand { get; }
    public Commands.RelayCommand OpenLedgerCommand { get; }
    public Commands.RelayCommand AddTransactionCmd { get; }
    public Commands.RelayCommand ExportSinglePartnerCmd { get; }
    public ObservableCollection<MenuItemViewModel> DynamicContextMenuItems { get; } = new();
    
    private PartnerRowDto? _selectedPartner;
    public PartnerRowDto? SelectedPartner
    {
        get => _selectedPartner;
        set
        {
            if (SetProperty(ref _selectedPartner, value))
            {
                // R-090: Notify commands to re-evaluate CanExecute when selection changes
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                OpenStatementCommand.RaiseCanExecuteChanged();
                AddTransactionCmd.RaiseCanExecuteChanged();
                ExportSinglePartnerCmd.RaiseCanExecuteChanged();
                RebuildContextMenuItems();
            }
        }
    }
    
    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                // R-090: Only search if empty (show all) or 3+ characters
                if (string.IsNullOrWhiteSpace(value) || value.Length >= 3)
                {
                    _ = RefreshAsync();
                }
            }
        }
    }
    
    public int PartnerCount => Partners.Count;
    
    public PartnerListViewModel(IPartnerService partnerService, IPartnerReadService partnerReadService, Abstractions.IDialogService dialogService,
        IExcelImportService excelImportService, Abstractions.IFileDialogService fileDialogService,
        IExcelExportService excelExportService, IListPdfExportService listPdfExportService, IPartnerExportService partnerExportService)
    {
        _partnerService = partnerService ?? throw new ArgumentNullException(nameof(partnerService));
        _partnerReadService = partnerReadService ?? throw new ArgumentNullException(nameof(partnerReadService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _excelImportService = excelImportService ?? throw new ArgumentNullException(nameof(excelImportService));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _excelExportService = excelExportService ?? throw new ArgumentNullException(nameof(excelExportService));
        _listPdfExportService = listPdfExportService ?? throw new ArgumentNullException(nameof(listPdfExportService));
        _partnerExportService = partnerExportService ?? throw new ArgumentNullException(nameof(partnerExportService));
        
        RefreshCommand = new Commands.RelayCommand(_ => { _ = RefreshAsync(); });
        NewCommand = new Commands.RelayCommand(_ => { _ = NewPartnerAsync(); });
        EditCommand = new Commands.RelayCommand(_ => { _ = EditPartnerAsync(); }, _ => SelectedPartner != null);
        DeleteCommand = new Commands.RelayCommand(_ => { _ = DeletePartnerAsync(); }, _ => SelectedPartner != null);
        ImportFromExcelCommand = new Commands.RelayCommand(_ => { _ = ImportFromExcelAsync(); });
        ExportListToExcelCommand = new Commands.RelayCommand(_ => { _ = ExportListToExcelAsync(); }, _ => Partners.Count > 0);
        ExportListToPdfCommand = new Commands.RelayCommand(_ => { _ = ExportListToPdfAsync(); }, _ => Partners.Count > 0);
        OpenStatementCommand = new Commands.RelayCommand(p => { _ = OpenStatementAsync(p); }, p => p is PartnerRowDto || SelectedPartner != null);
        OpenLedgerCommand = new Commands.RelayCommand(
            p => { _ = OpenLedgerAsync(p); }, 
            p => p is PartnerRowDto || SelectedPartner != null
        );
        AddTransactionCmd = new Commands.RelayCommand(async _ => await AddTransactionAsync(), _ => SelectedPartner != null);
        ExportSinglePartnerCmd = new Commands.RelayCommand(async _ => await ExportSinglePartnerAsync(), _ => SelectedPartner != null);

        RebuildContextMenuItems();
        // Initial load
        _ = RefreshAsync();
    }
    
    private async Task RefreshAsync()
    {
        try
        {
            Partners.Clear();

            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
            var page = 1;
            while (true)
            {
                var batch = await _partnerReadService.GetListAsync(search, page, LedgerPageSize);
                if (batch.Count == 0)
                    break;

                foreach (var partner in batch)
                {
                    Partners.Add(partner);
                }

                if (batch.Count < LedgerPageSize)
                    break;

                page++;
            }

            OnPropertyChanged(nameof(PartnerCount));
            ExportListToExcelCommand.RaiseCanExecuteChanged();
            ExportListToPdfCommand.RaiseCanExecuteChanged();
            OpenStatementCommand.RaiseCanExecuteChanged();
            RebuildContextMenuItems();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Cari listesi yÃ¼klenemedi: {ex.Message}", "Hata", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private async Task NewPartnerAsync()
    {
        try
        {
            var newDto = new PartnerCrudDetailDto
            {
                Id = 0,
                PartnerType = PartnerType.Customer.ToString(),
                Name = string.Empty,
                IsActive = true
            };
            
            var editViewModel = new PartnerEditViewModel(_partnerService, _partnerReadService, _excelExportService, _listPdfExportService, _fileDialogService, newDto);
            var dialog = new Views.PartnerEditDialog(editViewModel);
            if (dialog.ShowDialog() == true)
            {
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Yeni cari oluÅŸturulamadÄ±: {ex.Message}", "Hata", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private async Task EditPartnerAsync()
    {
        if (SelectedPartner == null) return;
        
        try
        {
            var dto = await _partnerService.GetByIdAsync(SelectedPartner.Id);
            if (dto == null)
            {
                System.Windows.MessageBox.Show("Cari bulunamadÄ±", "Hata", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            var editViewModel = new PartnerEditViewModel(_partnerService, _partnerReadService, _excelExportService, _listPdfExportService, _fileDialogService, dto);
            var dialog = new Views.PartnerEditDialog(editViewModel);
            if (dialog.ShowDialog() == true)
            {
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Cari dÃ¼zenlenemedi: {ex.Message}", "Hata", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private async Task DeletePartnerAsync()
    {
        if (SelectedPartner == null) return;
        
        var result = System.Windows.MessageBox.Show(
            $"'{SelectedPartner.Name}' adlÄ± cariyi silmek istediÄŸinize emin misiniz?",
            "Onay",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (result != System.Windows.MessageBoxResult.Yes) return;
        
        try
        {
            await _partnerService.DeleteAsync(SelectedPartner.Id);
            await RefreshAsync();
            
            System.Windows.MessageBox.Show("Cari silindi", "BaÅŸarÄ±lÄ±", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Cari silinemedi: {ex.Message}", "Hata", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private Task OpenStatementAsync(object? parameter = null)
    {
        var partner = (parameter as PartnerRowDto) ?? SelectedPartner;
        if (partner == null)
        {
            System.Windows.MessageBox.Show("Hareket dÃ¶kÃ¼mÃ¼ iÃ§in cari seÃ§iniz.", "UyarÄ±",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return Task.CompletedTask;
        }

        try
        {
            var dialog = new Views.PartnerStatementDialog(partner.Id, _partnerReadService, _partnerExportService)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Cari hareket dÃ¶kÃ¼mÃ¼ aÃ§Ä±lamadÄ±: {ex.Message}", "Hata",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// R-148: Open PartnerEditDialog directly to Hareketler (Ledger) tab
    /// </summary>
    private async Task OpenLedgerAsync(object? parameter = null)
    {
        var partner = (parameter as PartnerRowDto) ?? SelectedPartner;
        if (partner == null)
        {
            System.Windows.MessageBox.Show("Hareketler iÃ§in cari seÃ§iniz.", "UyarÄ±",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var dto = await _partnerService.GetByIdAsync(partner.Id);
            if (dto == null)
            {
                System.Windows.MessageBox.Show("Cari bulunamadÄ±", "Hata", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var editViewModel = new PartnerEditViewModel(_partnerService, _partnerReadService, _excelExportService, _listPdfExportService, _fileDialogService, dto, selectedTab: "Hareketler");
            var dialog = new Views.PartnerEditDialog(editViewModel)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            
            if (dialog.ShowDialog() == true)
            {
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Cari hareket sekmesi aÃ§Ä±lamadÄ±: {ex}", "Hata",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// R-093: Import partners from Excel file
    /// </summary>
    private async Task ImportFromExcelAsync()
    {
        try
        {
            // Show file picker
            var filePath = _fileDialogService.ShowOpenFileDialog(
                "Excel DosyalarÄ± (*.xlsx)|*.xlsx",
                "Cari Ä°Ã§eri Aktar");
            
            if (string.IsNullOrEmpty(filePath))
                return; // User cancelled
            
            // Import and get result
            var result = await _excelImportService.ImportPartnersAsync(filePath);
            
            // Refresh list
            await RefreshAsync();
            
            // Show result
            var icon = result.Success 
                ? System.Windows.MessageBoxImage.Information 
                : System.Windows.MessageBoxImage.Warning;
            
            System.Windows.MessageBox.Show(result.Summary, "Ä°Ã§eri Aktarma Sonucu", 
                System.Windows.MessageBoxButton.OK, icon);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Excel iÃ§eri aktarma baÅŸarÄ±sÄ±z: {ex.Message}", "Hata", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddTransactionAsync()
    {
        if (SelectedPartner == null)
        {
            System.Windows.MessageBox.Show("Cari seçilmedi.", "Uyarı",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        // R-226 FIX: THE TWIN VIEWMODEL FIX
        // This was the ACTUAL bug - hardcoded Receipt dialog for ALL partners!
        var role = SelectedPartner.Role ?? "";
        
        Serilog.Log.Information(">>> [R-226] LIST VIEW ACTION: Id={Id}, Name={Name}, Role={Role}", 
            SelectedPartner.Id, SelectedPartner.Name, role);
        
        // LOGIC: Default to PAYMENT unless strictly Customer
        bool isStrictlyCustomer = role.Equals("Customer", StringComparison.OrdinalIgnoreCase) ||
                                 role.Equals("Müşteri", StringComparison.OrdinalIgnoreCase);
        
        Serilog.Log.Information(">>> [R-226] isStrictlyCustomer={IsCustomer} -> Opening {Dialog}", 
            isStrictlyCustomer, isStrictlyCustomer ? "RECEIPT" : "PAYMENT");

        bool ok;
        if (isStrictlyCustomer)
        {
            // Customer -> Receipt (Tahsilat)
            Serilog.Log.Information(">>> [R-226] Opening RECEIPT for Customer");
            ok = await _dialogService.ShowCashReceiptDialogAsync();
        }
        else
        {
            // VENDOR / BOTH / OTHER / SUPPLIER -> PAYMENT (Ödeme)
            Serilog.Log.Information(">>> [R-226] Opening PAYMENT for Vendor/Supplier");
            ok = await _dialogService.ShowCashPaymentDialogAsync();
        }
        
        if (ok)
        {
            await RefreshAsync();
        }
    }

    private async Task ExportSinglePartnerAsync()
    {
        if (SelectedPartner == null)
        {
            System.Windows.MessageBox.Show("Cari seçilmedi.", "Uyarı",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var detail = await _partnerService.GetByIdAsync(SelectedPartner.Id);
            if (detail == null)
            {
                System.Windows.MessageBox.Show("Cari detayları alınamadı.", "Hata",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var filePath = _fileDialogService.ShowSaveFileDialog(
                $"Cari_{SelectedPartner.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                "Excel Dosyaları (*.xlsx)|*.xlsx",
                "Cari Kartı Dışa Aktar");

            if (string.IsNullOrWhiteSpace(filePath))
                return;

            _excelExportService.ExportToExcel(new[] { detail }, filePath, "Cari");
            System.Windows.MessageBox.Show($"Cari kartı Excel'e aktarıldı:\n{filePath}", "Başarılı",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Cari dışa aktarma başarısız: {ex.Message}", "Hata",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// R-095/R-123: Export partner list to Excel with full details
    /// </summary>
    private async Task ExportListToExcelAsync()
    {
        try
        {
            if (Partners.Count == 0)
            {
                System.Windows.MessageBox.Show("DÄ±ÅŸa aktarÄ±lacak cari bulunamadÄ±.", "UyarÄ±", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Show save file dialog - R-123: Fixed parameter order (defaultFileName, filter, title)
            var filePath = _fileDialogService.ShowSaveFileDialog(
                $"CariListesi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                "Excel DosyalarÄ± (*.xlsx)|*.xlsx",
                "Cari Listesi DÄ±ÅŸa Aktar");
            
            // R-157: Check if user cancelled the dialog
            if (string.IsNullOrEmpty(filePath))
            {
                return; // User cancelled
            }

            var detailList = new List<PartnerCrudDetailDto>();
            foreach (var partner in Partners)
            {
                var detail = await _partnerService.GetByIdAsync(partner.Id);
                if (detail != null)
                    detailList.Add(detail);
            }

            // Export to Excel
            await Task.Run(() => _excelExportService.ExportToExcel(detailList, filePath));

            System.Windows.MessageBox.Show($"Cari listesi Excel'e aktarÄ±ldÄ±:\n{filePath}", "BaÅŸarÄ±lÄ±", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Excel dÄ±ÅŸa aktarma baÅŸarÄ±sÄ±z: {ex.Message}", "Hata", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// R-095/R-123: Export partner list to PDF with full details
    /// </summary>
    private async Task ExportListToPdfAsync()
    {
        try
        {
            if (Partners.Count == 0)
            {
                System.Windows.MessageBox.Show("DÄ±ÅŸa aktarÄ±lacak cari bulunamadÄ±.", "UyarÄ±", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Show save file dialog - R-123: Fixed parameter order (defaultFileName, filter, title)
            var filePath = _fileDialogService.ShowSaveFileDialog(
                $"CariListesi_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                "PDF DosyalarÄ± (*.pdf)|*.pdf",
                "Cari Listesi DÄ±ÅŸa Aktar");

            if (string.IsNullOrEmpty(filePath))
                return; // User cancelled

            // R-123: Fetch full detail DTOs for complete export (all 16 columns)
            var detailList = new List<PartnerCrudDetailDto>();
            foreach (var partner in Partners)
            {
                var detail = await _partnerService.GetByIdAsync(partner.Id);
                if (detail != null)
                    detailList.Add(detail);
            }

            // Export to PDF
            await Task.Run(() => _listPdfExportService.ExportToPdf(detailList, filePath, "Cari Listesi"));

            System.Windows.MessageBox.Show($"Cari listesi PDF'e aktarÄ±ldÄ±:\n{filePath}", "BaÅŸarÄ±lÄ±", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"PDF dÄ±ÅŸa aktarma baÅŸarÄ±sÄ±z: {ex.Message}", "Hata", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}





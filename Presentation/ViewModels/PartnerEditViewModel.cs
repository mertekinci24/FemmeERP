// ReSharper disable once All
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Export;
using InventoryERP.Application.Partners;
using InventoryERP.Domain.Enums;
using InventoryERP.Presentation.Abstractions;

namespace InventoryERP.Presentation.ViewModels;

/// <summary>
/// R-086: Partner (Cari) Edit ViewModel
/// R-158: Added Ledger Export to Excel/PDF
/// </summary>
public sealed class PartnerEditViewModel : ViewModelBase
{
    private readonly IPartnerService _partnerService;
    private readonly IPartnerReadService _partnerReadService;
    private readonly IExcelExportService _excelExportService;
    private readonly IListPdfExportService _listPdfExportService;
    private readonly IFileDialogService _fileDialogService;
    private readonly PartnerCrudDetailDto _originalDto;
    private bool _ledgerLoaded;
    
    public List<PartnerTypeItem> PartnerTypes { get; }
    
    public bool IsNewPartner => _originalDto.Id == 0;
    
    private string _partnerType;
    public string PartnerType
    {
        get => _partnerType;
        set => SetProperty(ref _partnerType, value);
    }
    
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    private string? _taxId;
    public string? TaxId
    {
        get => _taxId;
        set => SetProperty(ref _taxId, value);
    }
    
    private string? _nationalId;
    public string? NationalId
    {
        get => _nationalId;
        set => SetProperty(ref _nationalId, value);
    }
    
    private string? _address;
    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }
    
    
    private int? _paymentTermDays;
    public int? PaymentTermDays
    {
        get => _paymentTermDays;
        set => SetProperty(ref _paymentTermDays, value);
    }
    
    private decimal? _creditLimitTry;
    public decimal? CreditLimitTry
    {
        get => _creditLimitTry;
        set => SetProperty(ref _creditLimitTry, value);
    }
    
    
    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
    
    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<PartnerStatementRowDto> LedgerEntries { get; } = new();

    private bool _isLedgerLoading;
    public bool IsLedgerLoading
    {
        get => _isLedgerLoading;
        set => SetProperty(ref _isLedgerLoading, value);
    }

    private string? _ledgerStatusMessage;
    public string? LedgerStatusMessage
    {
        get => _ledgerStatusMessage;
        set => SetProperty(ref _ledgerStatusMessage, value);
    }

    private decimal _ledgerEndingBalance;
    public decimal LedgerEndingBalance
    {
        get => _ledgerEndingBalance;
        set => SetProperty(ref _ledgerEndingBalance, value);
    }

    private string? _ledgerSummary;
    public string? LedgerSummary
    {
        get => _ledgerSummary;
        set => SetProperty(ref _ledgerSummary, value);
    }

    private string? _selectedTab;
    /// <summary>
    /// R-148: Tab to select when dialog opens ("Hareketler" to open ledger tab)
    /// </summary>
    public string? SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }
    
    /// <summary>
    /// R-158: Export ledger entries to Excel
    /// </summary>
    public Commands.AsyncRelayCommand ExportLedgerToExcelCommand { get; }
    
    /// <summary>
    /// R-158: Export ledger entries to PDF
    /// </summary>
    public Commands.AsyncRelayCommand ExportLedgerToPdfCommand { get; }
    
    public PartnerEditViewModel(
        IPartnerService partnerService, 
        IPartnerReadService partnerReadService,
        IExcelExportService excelExportService,
        IListPdfExportService listPdfExportService,
        IFileDialogService fileDialogService,
        PartnerCrudDetailDto dto, 
        string? selectedTab = null)
    {
        _partnerService = partnerService ?? throw new ArgumentNullException(nameof(partnerService));
        _partnerReadService = partnerReadService ?? throw new ArgumentNullException(nameof(partnerReadService));
        _excelExportService = excelExportService ?? throw new ArgumentNullException(nameof(excelExportService));
        _listPdfExportService = listPdfExportService ?? throw new ArgumentNullException(nameof(listPdfExportService));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _originalDto = dto ?? throw new ArgumentNullException(nameof(dto));
        
        // Initialize PartnerTypes list
        PartnerTypes = new List<PartnerTypeItem>
        {
            new PartnerTypeItem { Value = InventoryERP.Domain.Enums.PartnerType.Customer.ToString(), Display = "Müşteri" },
            new PartnerTypeItem { Value = InventoryERP.Domain.Enums.PartnerType.Supplier.ToString(), Display = "Tedarikçi" },
            new PartnerTypeItem { Value = InventoryERP.Domain.Enums.PartnerType.Other.ToString(), Display = "Diğer" }
        };
        
        // Load from DTO
        _partnerType = dto.PartnerType ??InventoryERP.Domain.Enums.PartnerType.Customer.ToString();
        _name = dto.Name ?? string.Empty;
        _taxId = dto.TaxId;
        _nationalId = dto.NationalId;
    _address = dto.Address;
        _paymentTermDays = dto.PaymentTermDays;
        _creditLimitTry = dto.CreditLimitTry;
        _isActive = dto.IsActive;
        LedgerStatusMessage = IsNewPartner
            ? "Yeni cari kartı oluşturulurken hareket bulunmaz."
            : "Hareketler yükleniyor...";
        
        // R-148: Set selected tab if specified
        _selectedTab = selectedTab;
        
        // R-158: Initialize Export Commands
        ExportLedgerToExcelCommand = new Commands.AsyncRelayCommand(ExportLedgerToExcelAsync, () => LedgerEntries.Count > 0);
        ExportLedgerToPdfCommand = new Commands.AsyncRelayCommand(ExportLedgerToPdfAsync, () => LedgerEntries.Count > 0);
    }
    
    public async Task<bool> SaveAsync()
    {
        try
        {
            ErrorMessage = null;
            
            // Client-side validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Cari adı zorunludur";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(TaxId) && string.IsNullOrWhiteSpace(NationalId))
            {
                ErrorMessage = "VKN veya TCKN zorunludur";
                return false;
            }
            
            if (!string.IsNullOrWhiteSpace(TaxId) && TaxId.Length != 10)
            {
                ErrorMessage = "VKN 10 haneli olmalıdır";
                return false;
            }
            
            if (!string.IsNullOrWhiteSpace(NationalId) && NationalId.Length != 11)
            {
                ErrorMessage = "TCKN 11 haneli olmalıdır";
                return false;
            }
            
            // Create DTO
            var dto = new PartnerCrudDetailDto
            {
                Id = _originalDto.Id,
                PartnerType = PartnerType,
                Name = Name.Trim(),
                TaxId = string.IsNullOrWhiteSpace(TaxId) ? null : TaxId.Trim(),
                NationalId = string.IsNullOrWhiteSpace(NationalId) ? null : NationalId.Trim(),
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                PaymentTermDays = PaymentTermDays,
                CreditLimitTry = CreditLimitTry,
                IsActive = IsActive
            };
            
            await _partnerService.SaveAsync(dto);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    public async Task LoadLedgerAsync(bool forceReload = false)
    {
        if (_ledgerLoaded && !forceReload)
            return;

        if (IsNewPartner)
        {
            LedgerEntries.Clear();
            LedgerSummary = "Yeni kaydedilen carilerde hareket bulunmaz.";
            LedgerEndingBalance = 0m;
            LedgerStatusMessage = "Henüz hareket yok.";
            _ledgerLoaded = true;
            return;
        }

        try
        {
            IsLedgerLoading = true;
            LedgerStatusMessage = "Hareketler yükleniyor...";

            var statement = await _partnerReadService.BuildStatementAsync(_originalDto.Id, null, null);

            LedgerEntries.Clear();
            foreach (var row in statement.Rows)
            {
                LedgerEntries.Add(row);
            }

            LedgerEndingBalance = statement.EndingBalance;
            LedgerSummary = $"Toplam Borç: {statement.TotalDebit:N2}   Toplam Alacak: {statement.TotalCredit:N2}   Bakiye: {statement.EndingBalance:N2}";
            LedgerStatusMessage = statement.Rows.Count == 0
                ? "Henüz hareket yok."
                : $"{statement.Rows.Count} hareket listelendi.";
            _ledgerLoaded = true;
            
            // R-158: Update export commands' CanExecute
            ExportLedgerToExcelCommand.RaiseCanExecuteChanged();
            ExportLedgerToPdfCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            LedgerStatusMessage = $"Hareketler yüklenemedi: {ex.Message}";
        }
        finally
        {
            IsLedgerLoading = false;
        }
    }
    
    /// <summary>
    /// R-158: Export ledger entries to Excel
    /// </summary>
    private async Task ExportLedgerToExcelAsync()
    {
        try
        {
            if (LedgerEntries.Count == 0)
            {
                System.Windows.MessageBox.Show("Dışa aktarılacak hareket bulunamadı.", "Uyarı",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // R-123: Show save file dialog
            var filePath = _fileDialogService.ShowSaveFileDialog(
                $"CariEkstre_{_originalDto.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                "Excel Dosyaları (*.xlsx)|*.xlsx",
                "Cari Ekstre Dışa Aktar");

            // R-157: Check if user cancelled
            if (string.IsNullOrEmpty(filePath))
            {
                return; // User cancelled
            }

            // Export to Excel using R-095 service
            var ledgerList = LedgerEntries.ToList();
            await Task.Run(() => _excelExportService.ExportToExcel(ledgerList, filePath, "Cari Ekstre"));

            System.Windows.MessageBox.Show($"Cari ekstre Excel'e aktarıldı:\n{filePath}", "Başarılı",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Excel dışa aktarma başarısız: {ex.Message}", "Hata",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// R-158: Export ledger entries to PDF
    /// </summary>
    private async Task ExportLedgerToPdfAsync()
    {
        try
        {
            if (LedgerEntries.Count == 0)
            {
                System.Windows.MessageBox.Show("Dışa aktarılacak hareket bulunamadı.", "Uyarı",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // R-123: Show save file dialog
            var filePath = _fileDialogService.ShowSaveFileDialog(
                $"CariEkstre_{_originalDto.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                "PDF Dosyaları (*.pdf)|*.pdf",
                "Cari Ekstre Dışa Aktar");

            // R-157: Check if user cancelled
            if (string.IsNullOrEmpty(filePath))
            {
                return; // User cancelled
            }

            // Export to PDF using R-095 service
            var ledgerList = LedgerEntries.ToList();
            await Task.Run(() => _listPdfExportService.ExportToPdf(ledgerList, filePath, "Cari Ekstre"));

            System.Windows.MessageBox.Show($"Cari ekstre PDF'e aktarıldı:\n{filePath}", "Başarılı",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"PDF dışa aktarma başarısız: {ex.Message}", "Hata",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}

public class PartnerTypeItem
{
    public string Value { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
}





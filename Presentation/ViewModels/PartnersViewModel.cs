using InventoryERP.Presentation.Commands;
// ReSharper disable once All
#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using InventoryERP.Application.Partners;
using System.Linq;

namespace InventoryERP.Presentation.ViewModels
{
public sealed class PartnersViewModel : ViewModelBase
{
    // PartnerEditDialog iÃ§in property ve komut
    public PartnerDetailDto EditModel { get => _editModel; set => SetProperty(ref _editModel, value); }
    private PartnerDetailDto _editModel = new(0, "", "Customer", "", 0, 0);
    public RelayCommand SaveCommand { get; }
    private readonly IPartnerReadService _readSvc;
    private readonly IPartnerCommandService _cmdSvc;
    private readonly InventoryERP.Application.Partners.IPartnerExportService _exportSvc;
    private readonly FluentValidation.IValidator<PartnerDetailDto> _validator;
    private readonly Abstractions.IDialogService _dialogService; // R-218: For Receipt/Payment dialogs

    public ObservableCollection<PartnerRowDto> Rows { get; } = new();
    public PartnerRowDto? Selected { get => _selected; set => SetProperty(ref _selected, value); }
    private PartnerRowDto? _selected;

    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) DebouncedLoad(); } }
    private string _searchText = "";
    public int PageSize { get; set; } = 100;
    public int Page { get => _page; set { if (SetProperty(ref _page, value)) _ = LoadAsync(); } }
    private int _page = 1;
    public int TotalCount { get => _totalCount; set => SetProperty(ref _totalCount, value); }
    private int _totalCount;
    public int RowCount => Rows.Count;

    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
    private string? _errorMessage;

    public RelayCommand LoadCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand OpenStatementCommand { get; }
    public RelayCommand OpenAgingCommand { get; }
    public RelayCommand ExportExcelCommand { get; }
    public RelayCommand ImportExcelCommand { get; }
    // aliases used from XAML
    public RelayCommand StatementCommand => OpenStatementCommand;
    public RelayCommand AgingCommand => OpenAgingCommand;
    public RelayCommand ExportCommand => ExportExcelCommand;
    public RelayCommand AddCashTransactionCommand { get; } // R-218: Smart Receipt/Payment routing

    private System.Threading.CancellationTokenSource? _searchCts;

    public PartnersViewModel(IPartnerReadService readSvc, IPartnerCommandService cmdSvc,InventoryERP.Application.Partners.IPartnerExportService exportSvc, FluentValidation.IValidator<PartnerDetailDto> validator, Abstractions.IDialogService dialogService)
    {
    SaveCommand = new RelayCommand(async _ => await SaveAsync());
        _readSvc = readSvc;
        _cmdSvc = cmdSvc;
        _exportSvc = exportSvc;
        _validator = validator;

    LoadCommand = new RelayCommand(async _ => await LoadAsync());
    NewCommand = new RelayCommand(async _ => await NewAsync());
    EditCommand = new RelayCommand(async _ => await EditAsync());
    DeleteCommand = new RelayCommand(async _ => await DeleteAsync());
    OpenStatementCommand = new RelayCommand(async _ => await OpenStatementAsync());
    OpenAgingCommand = new RelayCommand(async _ => await OpenAgingAsync());
    ExportExcelCommand = new RelayCommand(async _ => await ExportExcelAsync());
    ImportExcelCommand = new RelayCommand(_ => ImportExcel());
    AddCashTransactionCommand = new RelayCommand(async _ => await AddCashTransactionAsync()); // R-218
    _dialogService = dialogService; // R-218
    }

    private async Task LoadAsync()
    {
        _searchCts?.Cancel();
        Rows.Clear();
        var rows = await _readSvc.GetListAsync(SearchText, Page, PageSize);
        foreach (var x in rows) Rows.Add(x);
        TotalCount = await _readSvc.GetTotalCountAsync(SearchText);
    }

    private async void DebouncedLoad()
    {
        _searchCts?.Cancel();
        _searchCts = new System.Threading.CancellationTokenSource();
        var token = _searchCts.Token;
        await Task.Delay(500);
        if (!token.IsCancellationRequested) await LoadAsync();
    }

    private Task NewAsync()
    {
        // PartnerEditDialog aÃ§Ä±lÄ±r, EditModel doldurulur
        EditModel = new PartnerDetailDto(0, "", "Customer", "", 0, 0);
        ErrorMessage = null;
        // Dialog aÃ§Ä±lÄ±r (Ã¶rnek: DialogService.ShowDialog(EditModel, SaveCommand))
        return Task.CompletedTask;
    }

    private Task EditAsync()
    {
        if (Selected is null) { ErrorMessage = "DÃ¼zenlenecek cari seÃ§ilmedi."; return Task.CompletedTask; }
        EditModel = new PartnerDetailDto(Selected.Id, Selected.Name, Selected.Role, Selected.TaxNo, Selected.BalanceTry, Selected.CreditLimitTry);
        ErrorMessage = null;
        // Dialog aÃ§Ä±lÄ±r
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        var result = _validator.Validate(EditModel);
        if (!result.IsValid)
        {
            ErrorMessage = string.Join("\n", result.Errors.Select(e => e.ErrorMessage));
            return;
        }
        if (EditModel.Id == 0)
            await _cmdSvc.CreateAsync(EditModel);
        else
            await _cmdSvc.UpdateAsync(EditModel);
        await LoadAsync();
        ErrorMessage = null;
        // Dialog kapatÄ±lÄ±r
    }

    private Task DeleteAsync()
    {
        // TODO: Onay, _cmdSvc.DeleteAsync, LoadAsync
        return Task.CompletedTask;
    }

    private Task OpenStatementAsync()
    {
        if (Selected is null) { ErrorMessage = "SeÃ§ili cari bulunmuyor."; return Task.CompletedTask; }
    var dlg = new Views.PartnerStatementDialog(Selected.Id, _readSvc, _exportSvc);
        dlg.Owner = System.Windows.Application.Current?.MainWindow;
        dlg.ShowDialog();
        return Task.CompletedTask;
    }

    private Task OpenAgingAsync()
    {
        if (Selected is null) { ErrorMessage = "SeÃ§ili cari bulunmuyor."; return Task.CompletedTask; }
        var dlg = new Views.PartnerAgingDialog(Selected.Id, _readSvc, _exportSvc);
        dlg.Owner = System.Windows.Application.Current?.MainWindow;
        dlg.ShowDialog();
        return Task.CompletedTask;
    }

    private async Task ExportExcelAsync()
    {
        if (Selected is null) { ErrorMessage = "SeÃ§ili cari bulunmuyor."; return; }
        var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx|PDF File (*.pdf)|*.pdf", FileName = $"cari_ekstre_{Selected.Id}_{DateTime.UtcNow:yyyyMMdd}.xlsx" };
        if (sfd.ShowDialog() == true)
        {
            try
            {
                var ext = System.IO.Path.GetExtension(sfd.FileName)?.ToLowerInvariant();
                if (ext == ".pdf")
                {
                    var bytes = await _exportSvc.ExportStatementPdfAsync(Selected.Id, null, null, includeClosed: true);
                    await System.IO.File.WriteAllBytesAsync(sfd.FileName, bytes);
                }
                else
                {
                    var bytes = await _exportSvc.ExportStatementExcelAsync(Selected.Id, null, null);
                    await System.IO.File.WriteAllBytesAsync(sfd.FileName, bytes);
                }
                ErrorMessage = "DÄ±ÅŸa aktarÄ±m tamamlandÄ±.";
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"DÄ±ÅŸa aktarÄ±m hatasÄ±: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ImportExcel()
    {
        // TODO: PartnerExcelImporter.Import
    }

    // R-218: Smart Cash Transaction Routing
    // Customer -> Receipt (Tahsilat)
    // Vendor -> Payment (Ödeme)
    private async Task AddCashTransactionAsync()
    {
        if (Selected is null)
        {
            ErrorMessage = "Lütfen önce bir cari seçin.";
            return;
        }

        try
        {
            // R-225 FIX: INVERTED LOGIC - DEFAULT TO PAYMENT
            // Check for Customer first, EVERYTHING ELSE gets Payment
            var role = Selected.Role ?? "";
            
            // R-225 HIGH-DETAIL LOGGING: Inspect the partner object
            Serilog.Log.Information(">>> [R-225] INSPECTING PARTNER: Id={Id}, Name={Name}, RoleStr={Role}", 
                Selected.Id, Selected.Name, role);
            
            // R-225: Check if strictly a Customer (only Customer type gets Receipt)
            bool isStrictlyCustomer = role.Equals("Customer", StringComparison.OrdinalIgnoreCase) ||
                                     role.Equals("Müşteri", StringComparison.OrdinalIgnoreCase);
            
            Serilog.Log.Information(">>> [R-225] isStrictlyCustomer={IsCustomer} -> Will open {DialogType}", 
                isStrictlyCustomer, isStrictlyCustomer ? "RECEIPT (Tahsilat)" : "PAYMENT (Ödeme)");

            bool result;
            if (isStrictlyCustomer)
            {
                // ONLY Customer -> Receipt (Tahsilat)
                Serilog.Log.Information(">>> [R-225] Identified as CUSTOMER. Opening RECEIPT.");
                result = await _dialogService.ShowCashReceiptDialogAsync();
            }
            else
            {
                // EVERYTHING ELSE (Supplier/Vendor/Both/Other/Unknown) -> Payment (Ödeme)
                Serilog.Log.Information(">>> [R-225] NOT a Customer (Vendor/Both/Other). Opening PAYMENT.");
                result = await _dialogService.ShowCashPaymentDialogAsync();
            }

            if (result)
            {
                await LoadAsync(); // Refresh partner list to show updated balance
                Messaging.EventBus.RaiseCashTransactionSaved(); // R-219: Notify CashAccountsViewModel to refresh
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"İşlem açılamadı: {ex.Message}";
            MessageBox.Show(ErrorMessage, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
}





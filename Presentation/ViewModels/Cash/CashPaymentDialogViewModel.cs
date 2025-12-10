using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using InventoryERP.Application.Cash;
using InventoryERP.Application.Cash.DTOs;
using InventoryERP.Application.Partners;
using InventoryERP.Presentation.Commands;

namespace InventoryERP.Presentation.ViewModels.Cash;

/// <summary>
/// R-131: Cash Payment Dialog (Ödeme Fişi) ViewModel
/// </summary>
public class CashPaymentDialogViewModel : ViewModelBase
{
    private readonly ICashService _cashService;
    private readonly IPartnerReadService _partnerService;
    
    private ObservableCollection<CashAccountDto> _cashAccounts = new();
    private CashAccountDto? _selectedCashAccount;
    private ObservableCollection<PartnerRowDto> _partners = new();
    private PartnerRowDto? _selectedPartner;
    private DateTime _date = DateTime.Today;
    private decimal _amount;
    private string _description = string.Empty;
    private string? _errorMessage;

    public ObservableCollection<CashAccountDto> CashAccounts
    {
        get => _cashAccounts;
        set => SetProperty(ref _cashAccounts, value);
    }

    public CashAccountDto? SelectedCashAccount
    {
        get => _selectedCashAccount;
        set => SetProperty(ref _selectedCashAccount, value);
    }

    public ObservableCollection<PartnerRowDto> Partners
    {
        get => _partners;
        set => SetProperty(ref _partners, value);
    }

    public PartnerRowDto? SelectedPartner
    {
        get => _selectedPartner;
        set => SetProperty(ref _selectedPartner, value);
    }

    public DateTime Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public bool DialogResult { get; private set; }

    public CashPaymentDialogViewModel(ICashService cashService, IPartnerReadService partnerService)
    {
        _cashService = cashService;
        _partnerService = partnerService;
        
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => Cancel());
        
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var accountsTask = _cashService.GetAllAccountsAsync();
            var partnersTask = _partnerService.GetListAsync(null, 1, 1000); // Load all partners (limit 1000 for now)

            await Task.WhenAll(accountsTask, partnersTask);

            CashAccounts = new ObservableCollection<CashAccountDto>(await accountsTask);
            SelectedCashAccount = CashAccounts.FirstOrDefault();

            Partners = new ObservableCollection<PartnerRowDto>(await partnersTask);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Veriler yüklenemedi: {ex.Message}";
        }
    }

    private async Task SaveAsync()
    {
        if (SelectedCashAccount == null)
        {
            ErrorMessage = "Lütfen bir kasa hesabı seçin.";
            return;
        }

        if (Amount <= 0)
        {
            ErrorMessage = "Tutar sıfırdan büyük olmalıdır.";
            return;
        }

        try
        {
            var dto = new CashPaymentDto
            {
                CashAccountId = SelectedCashAccount.Id,
                PartnerId = SelectedPartner?.Id,
                Date = Date,
                Amount = Amount,
                Currency = SelectedCashAccount.Currency,
                FxRate = 1.0m,
                Description = Description
            };

            await _cashService.CreatePaymentAsync(dto);
            DialogResult = true;
            OnRequestClose();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ödeme kaydedilemedi: {ex.Message}";
        }
    }

    private void Cancel()
    {
        DialogResult = false;
        OnRequestClose();
    }

    public event EventHandler? RequestClose;

    protected virtual void OnRequestClose()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}

using System.Collections.ObjectModel;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Presentation.ViewModels.Cash;

/// <summary>
/// R-196.1: ViewModel for creating/editing cash or bank accounts.
/// </summary>
public class CashAccountEditDialogViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _currency = "TRY";
    private CashAccountType _type = CashAccountType.Cash;
    private string? _bankName;
    private string? _bankBranch;
    private string? _accountNumber;
    private string? _iban;
    private string? _swiftCode;
    private string? _description;

    public ObservableCollection<string> Currencies { get; } = new(new[] { "TRY", "USD", "EUR" });

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Currency
    {
        get => _currency;
        set => SetProperty(ref _currency, value);
    }

    public CashAccountType Type
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
            {
                OnPropertyChanged(nameof(IsBank));
                OnPropertyChanged(nameof(IsCash));
            }
        }
    }

    public bool IsBank => Type == CashAccountType.Bank;
    public bool IsCash
    {
        get => Type == CashAccountType.Cash;
        set
        {
            if (value)
            {
                Type = CashAccountType.Cash;
            }
        }
    }

    public bool BankSelected
    {
        get => Type == CashAccountType.Bank;
        set
        {
            if (value)
            {
                Type = CashAccountType.Bank;
            }
        }
    }

    public string? BankName
    {
        get => _bankName;
        set => SetProperty(ref _bankName, value);
    }

    public string? BankBranch
    {
        get => _bankBranch;
        set => SetProperty(ref _bankBranch, value);
    }

    public string? AccountNumber
    {
        get => _accountNumber;
        set => SetProperty(ref _accountNumber, value);
    }

    public string? Iban
    {
        get => _iban;
        set => SetProperty(ref _iban, value);
    }

    public string? SwiftCode
    {
        get => _swiftCode;
        set => SetProperty(ref _swiftCode, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
}

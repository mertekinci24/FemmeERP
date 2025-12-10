using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using InventoryERP.Application.Cash;
using InventoryERP.Application.Cash.DTOs;
using InventoryERP.Domain.Enums;
using InventoryERP.Presentation.Commands;
using InventoryERP.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.ViewModels.Cash;

/// <summary>
/// R-131: Cash Account List ViewModel
/// </summary>
public class CashAccountListViewModel : ViewModelBase
{
    private readonly ICashService _cashService;
    private readonly IServiceScopeFactory _scopeFactory;
    private ObservableCollection<CashAccountDto> _accounts = new();
    private bool _isLoading;
    private CashAccountDto? _selectedAccount;
    public CashAccountDto? SelectedAccount
    {
        get => _selectedAccount;
        set => SetProperty(ref _selectedAccount, value);
    }

    public RelayCommand CreateAccountCmd { get; }
    public RelayCommand OpenStatementCmd { get; }

    public ObservableCollection<CashAccountDto> Accounts
    {
        get => _accounts;
        set => SetProperty(ref _accounts, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public CashAccountListViewModel(ICashService cashService, IServiceScopeFactory scopeFactory)
    {
        _cashService = cashService;
        _scopeFactory = scopeFactory;
        CreateAccountCmd = new RelayCommand(async _ => await CreateAccountAsync());
        OpenStatementCmd = new RelayCommand(new Action<object?>((o) => OpenStatementDialog()));
        
        // R-219: Subscribe to EventBus to refresh when cash transactions are saved
        Messaging.EventBus.CashTransactionSaved += OnCashTransactionSaved;
        
        _ = RefreshAsync();
    }

    // R-219: Event handler for cash transaction saved
    private async void OnCashTransactionSaved(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            var accounts = await _cashService.GetAllAccountsAsync();
            Accounts = new ObservableCollection<CashAccountDto>(accounts);
        }
        catch (Exception ex)
        {
            // Log or handle error
            System.Diagnostics.Debug.WriteLine($"Error refreshing cash accounts: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenStatementDialog()
    {
        if (SelectedAccount == null) return;

        try
        {
            var vm = new CashStatementDialogViewModel(SelectedAccount.Id, SelectedAccount.Name, _cashService);
            var dlg = new CashStatementDialog
            {
                DataContext = vm,
                Owner = System.Windows.Application.Current.MainWindow
            };
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ekstre açılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task CreateAccountAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dialog = scope.ServiceProvider.GetRequiredService<CashAccountEditDialog>();

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
                // ignore owner assignment failures
            }

            if (dialog.ShowDialog() == true)
            {
                var vm = dialog.ViewModel;
                var dto = new CashAccountDto
                {
                    Name = vm.Name.Trim(),
                    Type = vm.Type,
                    Currency = vm.Currency,
                    BankName = vm.BankName?.Trim(),
                    BankBranch = vm.BankBranch?.Trim(),
                    AccountNumber = vm.AccountNumber?.Trim(),
                    Iban = vm.Iban?.Trim(),
                    SwiftCode = vm.SwiftCode?.Trim(),
                    Description = vm.Description?.Trim(),
                    IsActive = true
                };
                await _cashService.CreateAccountAsync(dto);
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hesap oluşturulamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}


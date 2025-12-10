using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Cash;
using InventoryERP.Application.Cash.DTOs;

namespace Tests.Unit.Fakes;

public class FakeCashService : ICashService
{
    public List<CashReceiptDto> Receipts { get; } = new();
    public List<CashPaymentDto> Payments { get; } = new();
    public List<CashAccountDto> Accounts { get; } = new()
    {
        new CashAccountDto { Id = 1, Name = "Kasa", Currency = "TRY", IsActive = true }
    };

    public Task<int> CreateAccountAsync(CashAccountDto dto) => Task.FromResult(2);
    public Task DeleteAccountAsync(int id) => Task.CompletedTask;
    public Task<List<CashAccountDto>> GetAllAccountsAsync() => Task.FromResult(Accounts);
    public Task<CashAccountDto?> GetAccountByIdAsync(int id) => Task.FromResult(Accounts.FirstOrDefault(a => a.Id == id));
    public Task<List<CashLedgerDto>> GetLedgerEntriesAsync(int cashAccountId, DateTime? f = null, DateTime? t = null) => Task.FromResult(new List<CashLedgerDto>());
    public Task<decimal> GetBalanceAsync(int cashAccountId, DateTime? asOf = null) => Task.FromResult(0m);
    public Task UpdateAccountAsync(CashAccountDto dto) => Task.CompletedTask;
    public Task<int> CreateReceiptAsync(CashReceiptDto dto) { Receipts.Add(dto); return Task.FromResult(1); }
    public Task<int> CreatePaymentAsync(CashPaymentDto dto) { Payments.Add(dto); return Task.FromResult(1); }
}



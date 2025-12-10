using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryERP.Application.Cash.DTOs;

namespace InventoryERP.Application.Cash;

/// <summary>
/// R-007/R-131: Cash management service interface.
/// </summary>
public interface ICashService
{
    Task<List<CashAccountDto>> GetAllAccountsAsync();
    Task<CashAccountDto?> GetAccountByIdAsync(int id);
    Task<int> CreateAccountAsync(CashAccountDto dto);
    Task UpdateAccountAsync(CashAccountDto dto);
    Task DeleteAccountAsync(int id);
    
    Task<List<CashLedgerDto>> GetLedgerEntriesAsync(int cashAccountId, DateTime? from = null, DateTime? to = null);
    Task<decimal> GetBalanceAsync(int cashAccountId, DateTime? asOfDate = null);
    
    Task<int> CreateReceiptAsync(CashReceiptDto dto);
    Task<int> CreatePaymentAsync(CashPaymentDto dto);
}

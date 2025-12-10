using InventoryERP.Application.Cash;
using InventoryERP.Application.Cash.DTOs;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Services;

/// <summary>
/// R-007/R-131: Cash management service implementation.
/// </summary>
public class CashService : ICashService
{
    private readonly AppDbContext _context;

    public CashService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CashAccountDto>> GetAllAccountsAsync()
    {
        var accounts = await _context.CashAccounts
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var result = new List<CashAccountDto>();
        foreach (var acc in accounts)
        {
            var balance = await GetBalanceAsync(acc.Id);
            result.Add(new CashAccountDto
            {
                Id = acc.Id,
                Name = acc.Name,
                Type = acc.Type,
                Currency = acc.Currency,
                BankName = acc.BankName,
                BankBranch = acc.BankBranch,
                AccountNumber = acc.AccountNumber,
                Iban = acc.Iban,
                SwiftCode = acc.SwiftCode,
                Description = acc.Description,
                IsActive = acc.IsActive,
                Balance = balance
            });
        }
        return result;
    }

    public async Task<CashAccountDto?> GetAccountByIdAsync(int id)
    {
        var acc = await _context.CashAccounts.FindAsync(id);
        if (acc == null) return null;

        var balance = await GetBalanceAsync(id);
        return new CashAccountDto
        {
            Id = acc.Id,
            Name = acc.Name,
            Type = acc.Type,
            Currency = acc.Currency,
            BankName = acc.BankName,
            BankBranch = acc.BankBranch,
            AccountNumber = acc.AccountNumber,
            Iban = acc.Iban,
            SwiftCode = acc.SwiftCode,
            Description = acc.Description,
            IsActive = acc.IsActive,
            Balance = balance
        };
    }

    public async Task<int> CreateAccountAsync(CashAccountDto dto)
    {
        if (dto.Type == CashAccountType.Bank && string.IsNullOrWhiteSpace(dto.Iban) && string.IsNullOrWhiteSpace(dto.AccountNumber))
        {
            throw new InvalidOperationException("Bank account requires IBAN or Account Number.");
        }

        var entity = new CashAccount
        {
            Name = dto.Name,
            Type = dto.Type,
            Currency = dto.Currency,
            BankName = dto.BankName,
            BankBranch = dto.BankBranch,
            AccountNumber = dto.AccountNumber,
            Iban = dto.Iban,
            SwiftCode = dto.SwiftCode,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        _context.CashAccounts.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAccountAsync(CashAccountDto dto)
    {
        var entity = await _context.CashAccounts.FindAsync(dto.Id);
        if (entity == null) throw new InvalidOperationException($"Cash account {dto.Id} not found");

        if (dto.Type == CashAccountType.Bank && string.IsNullOrWhiteSpace(dto.Iban) && string.IsNullOrWhiteSpace(dto.AccountNumber))
        {
            throw new InvalidOperationException("Bank account requires IBAN or Account Number.");
        }

        entity.Name = dto.Name;
        entity.Type = dto.Type;
        entity.Currency = dto.Currency;
        entity.BankName = dto.BankName;
        entity.BankBranch = dto.BankBranch;
        entity.AccountNumber = dto.AccountNumber;
        entity.Iban = dto.Iban;
        entity.SwiftCode = dto.SwiftCode;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(int id)
    {
        var entity = await _context.CashAccounts.FindAsync(id);
        if (entity == null) throw new InvalidOperationException($"Cash account {id} not found");

        var hasEntries = await _context.CashLedgerEntries.AnyAsync(e => e.CashAccountId == id);
        if (hasEntries)
        {
            throw new InvalidOperationException("Cannot delete cash account with existing ledger entries");
        }

        _context.CashAccounts.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<CashLedgerDto>> GetLedgerEntriesAsync(int cashAccountId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.CashLedgerEntries
            .Include(e => e.CashAccount)
            .Where(e => e.CashAccountId == cashAccountId);

        if (from.HasValue)
            query = query.Where(e => e.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.Date <= to.Value);

        var entries = await query
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Id)
            .ToListAsync();

        return entries.Select(e => new CashLedgerDto
        {
            Id = e.Id,
            CashAccountId = e.CashAccountId,
            CashAccountName = e.CashAccount?.Name ?? "",
            DocId = e.DocId,
            DocNumber = e.DocNumber,
            DocType = e.DocType,
            Date = e.Date,
            Currency = e.Currency,
            FxRate = e.FxRate,
            Debit = e.Debit,
            Credit = e.Credit,
            Balance = e.Balance,
            AmountTry = e.AmountTry,
            Description = e.Description,
            Status = e.Status
        }).ToList();
    }

    public async Task<decimal> GetBalanceAsync(int cashAccountId, DateTime? asOfDate = null)
    {
        var query = _context.CashLedgerEntries
            .Where(e => e.CashAccountId == cashAccountId && e.Status == LedgerStatus.OPEN);

        if (asOfDate.HasValue)
            query = query.Where(e => e.Date <= asOfDate.Value);

        var lastEntry = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync();

        return lastEntry?.Balance ?? 0;
    }
    public async Task<int> CreateReceiptAsync(CashReceiptDto dto)
    {
        // R-220 DIAGNOSTIC: Verify correct method is called
        Console.WriteLine($"[R-220] CreateReceiptAsync called - PartnerId={dto.PartnerId}, Amount={dto.Amount}");
        Console.WriteLine($"[R-220] RECEIPT: Cash Debit (IN), Partner Credit (debt decreases)");
        
        var account = await _context.CashAccounts.FindAsync(dto.CashAccountId);
        if (account == null) throw new InvalidOperationException($"Cash account {dto.CashAccountId} not found");

        var currentBalance = await GetBalanceAsync(dto.CashAccountId, dto.Date.AddDays(-1));
        var amountTry = dto.Amount * dto.FxRate;

        var entry = new CashLedgerEntry
        {
            CashAccountId = dto.CashAccountId,
            Date = dto.Date,
            Currency = dto.Currency,
            FxRate = dto.FxRate,
            Debit = dto.Amount,
            Credit = 0,
            Balance = currentBalance + dto.Amount,
            AmountTry = amountTry,
            Description = dto.Description,
            Status = LedgerStatus.OPEN,
            DocType = DocumentType.RECEIPT,
            DocNumber = $"TAH-{DateTime.Now:yyyyMMddHHmmss}"
        };

        _context.CashLedgerEntries.Add(entry);

        // R-207.1: Double Entry - Credit Partner (Alacak)
        if (dto.PartnerId.HasValue && dto.PartnerId.Value > 0)
        {
            _context.PartnerLedgerEntries.Add(new PartnerLedgerEntry
            {
                PartnerId = dto.PartnerId.Value,
                Date = dto.Date,
                DueDate = dto.Date, // Cash is immediate
                Currency = dto.Currency,
                FxRate = dto.FxRate,
                Debit = 0,
                Credit = dto.Amount,
                AmountTry = amountTry,
                Description = dto.Description ?? "Tahsilat Fişi",
                Status = LedgerStatus.OPEN,
                DocType = DocumentType.RECEIPT,
                DocNumber = entry.DocNumber
            });
        }

        await _context.SaveChangesAsync();

        // Update all subsequent balances
        await RecalculateBalancesAsync(dto.CashAccountId, dto.Date);

        return entry.Id;
    }

    public async Task<int> CreatePaymentAsync(CashPaymentDto dto)
    {
        // R-222: Use Serilog file logging (Console is invisible in WPF)
        Serilog.Log.Information(">>> [R-222] PAYMENT STARTED: PartnerId={PartnerId}, Amount={Amount}", dto.PartnerId, dto.Amount);
        
        var account = await _context.CashAccounts.FindAsync(dto.CashAccountId);
        if (account == null) throw new InvalidOperationException($"Cash account {dto.CashAccountId} not found");

        var currentBalance = await GetBalanceAsync(dto.CashAccountId, dto.Date.AddDays(-1));
        var amountTry = dto.Amount * dto.FxRate;

        var entry = new CashLedgerEntry
        {
            CashAccountId = dto.CashAccountId,
            Date = dto.Date,
            Currency = dto.Currency,
            FxRate = dto.FxRate,
            Debit = 0,
            Credit = dto.Amount,
            Balance = currentBalance - dto.Amount,
            AmountTry = amountTry,
            Description = dto.Description,
            Status = LedgerStatus.OPEN,
            DocType = DocumentType.PAYMENT,
            DocNumber = $"ODE-{DateTime.Now:yyyyMMddHHmmss}"
        };

        _context.CashLedgerEntries.Add(entry);
        Serilog.Log.Information(">>> [R-222] CASH LEDGER: Debit={Debit}, Credit={Credit} (Cash goes OUT)", entry.Debit, entry.Credit);

        // R-207.1: Double Entry - Debit Partner (Borç)
        // R-222 EXPLICIT VERIFICATION: Payment = Partner DEBIT (reduces vendor balance)
        if (dto.PartnerId.HasValue && dto.PartnerId.Value > 0)
        {
            var partnerEntry = new PartnerLedgerEntry
            {
                PartnerId = dto.PartnerId.Value,
                Date = dto.Date,
                DueDate = dto.Date, // Cash is immediate
                Currency = dto.Currency,
                FxRate = dto.FxRate,
                Debit = dto.Amount,  // R-222: PAYMENT -> Partner DEBIT (not Credit!)
                Credit = 0,          // R-222: Credit must be ZERO for payments
                AmountTry = amountTry,
                Description = dto.Description ?? "Ödeme Fişi",
                Status = LedgerStatus.OPEN,
                DocType = DocumentType.PAYMENT,
                DocNumber = entry.DocNumber
            };
            
            // R-223 FILE LOG: Log exact values being saved
            Serilog.Log.Information(">>> [R-223] WRITING TO DB: PartnerLedger -> Debit={Debit}, Credit={Credit}", partnerEntry.Debit, partnerEntry.Credit);
            
            // R-223 CRITICAL VALIDATION CHECK
            if (partnerEntry.Credit > 0)
            {
                Serilog.Log.Error(">>> [R-223] CRITICAL ERROR: Payment is writing to CREDIT (Alacak)! It should be DEBIT (Borç)!");
            }
            else
            {
                Serilog.Log.Information(">>> [R-223] LOGIC CHECK PASSED: Payment is writing to DEBIT. Credit={Credit}", partnerEntry.Credit);
            }
            
            _context.PartnerLedgerEntries.Add(partnerEntry);
        }

        await _context.SaveChangesAsync();
        Serilog.Log.Information(">>> [R-222] PAYMENT COMPLETED: DocNumber={DocNumber}", entry.DocNumber);

        // Update all subsequent balances
        await RecalculateBalancesAsync(dto.CashAccountId, dto.Date);

        return entry.Id;
    }

    private async Task RecalculateBalancesAsync(int cashAccountId, DateTime fromDate)
    {
        var entries = await _context.CashLedgerEntries
            .Where(e => e.CashAccountId == cashAccountId && e.Date >= fromDate && e.Status == LedgerStatus.OPEN)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Id)
            .ToListAsync();

        if (entries.Count == 0) return;

        var previousBalance = await GetBalanceAsync(cashAccountId, fromDate.AddDays(-1));

        foreach (var entry in entries)
        {
            previousBalance = previousBalance + entry.Debit - entry.Credit;
            entry.Balance = previousBalance;
        }

        await _context.SaveChangesAsync();
    }
}

using System;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Application.Cash.DTOs;

/// <summary>
/// R-131: Cash account DTO.
/// </summary>
public class CashAccountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CashAccountType Type { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? SwiftCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal Balance { get; set; }
}

/// <summary>
/// R-131: Cash ledger entry DTO.
/// </summary>
public class CashLedgerDto
{
    public int Id { get; set; }
    public int CashAccountId { get; set; }
    public string CashAccountName { get; set; } = string.Empty;
    public int? DocId { get; set; }
    public string? DocNumber { get; set; }
    public DocumentType? DocType { get; set; }
    public DateTime Date { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal FxRate { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public decimal AmountTry { get; set; }
    public string? Description { get; set; }
    public LedgerStatus Status { get; set; }
}

/// <summary>
/// R-131: Cash receipt (Tahsilat Fişi) DTO.
/// </summary>
public class CashReceiptDto
{
    public int CashAccountId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal FxRate { get; set; } = 1.0m;
    public string? Description { get; set; }
    public int? PartnerId { get; set; }
}

/// <summary>
/// R-131: Cash payment (Ödeme Fişi) DTO.
/// </summary>
public class CashPaymentDto
{
    public int CashAccountId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal FxRate { get; set; } = 1.0m;
    public string? Description { get; set; }
    public int? PartnerId { get; set; }
}

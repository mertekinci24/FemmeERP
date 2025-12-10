using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryERP.Application.Reports;

public record ThisMonthSalesContract(int Year, int Month, decimal TotalTry, int InvoiceCount);

public interface IDashboardQueries
{
    Task<ThisMonthSalesContract> GetThisMonthSalesAsync(int year, int month, CancellationToken ct = default);
    Task<OverdueReceivablesContract> GetOverdueReceivablesAsync(DateTime? asOf = null, int topN = 5, CancellationToken ct = default);
    Task<CashBankBalancesContract> GetCashBankBalancesAsync(DateTime? asOf = null, CancellationToken ct = default);
    Task<TopProductsContract> GetTopProductsAsync(DateTime? from = null, DateTime? to = null, int topN = 10, CancellationToken ct = default);
}

public record OverduePartnerDto(int PartnerId, string Name, decimal TotalOverdue);

public record OverdueReceivablesContract(decimal TotalOverdueTry, int OverduePartnerCount, List<OverduePartnerDto> TopPartners);

public record CashAccountBalanceDto(int PartnerId, string Name, decimal BalanceTry);

public record CashBankBalancesContract(decimal TotalCashBankBalanceTry, List<CashAccountBalanceDto> Accounts);

public record TopProductDto(int ProductId, string Sku, string Name, decimal Quantity, decimal RevenueTry);

public record TopProductsContract(List<TopProductDto> TopProducts);

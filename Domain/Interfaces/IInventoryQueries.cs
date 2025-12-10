namespace InventoryERP.Domain.Interfaces;

public interface IInventoryQueries
{
    Task<decimal> GetOnHandAsync(int itemId, CancellationToken ct = default);
    Task<decimal> GetPartnerBalanceAsync(int partnerId, CancellationToken ct = default);
    Task<decimal> GetAvailableAsync(int itemId, CancellationToken ct = default);
}

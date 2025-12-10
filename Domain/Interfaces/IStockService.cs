namespace InventoryERP.Domain.Interfaces;

public interface IStockService
{
    Task PostMoveAsync(int itemId, decimal qty, bool isInbound, string? note = null, CancellationToken ct = default);
}

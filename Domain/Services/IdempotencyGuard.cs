using InventoryERP.Domain.Entities;

namespace InventoryERP.Domain.Services;

public static class IdempotencyGuard
{
    public static bool IsExternalIdValid(string? externalId)
    {
        return !string.IsNullOrWhiteSpace(externalId);
    }
}

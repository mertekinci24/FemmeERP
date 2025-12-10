namespace InventoryERP.Domain.Services;

public static class InventoryService
{
    public static void EnsureStockNotNegative(decimal currentOnHand, decimal qtySigned)
    {
        var result = currentOnHand + qtySigned;
        if (result < 0)
            throw new InvalidOperationException("STK-NEG-001: On-hand cannot be negative.");
    }
}


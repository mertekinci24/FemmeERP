using InventoryERP.Domain.Services;
using FluentAssertions;

namespace Tests.Unit;

public class InventoryServiceTests
{
    [Fact]
    public void NegativeOnHand_Should_Throw()
    {
        Action act = () => InventoryService.EnsureStockNotNegative(1.0m, -2.0m);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NonNegativeOnHand_Should_NotThrow()
    {
        Action act = () => InventoryService.EnsureStockNotNegative(5.0m, -3.0m);
        act.Should().NotThrow();
    }
}


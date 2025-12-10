using System.Threading.Tasks;
using InventoryERP.Application.Stocks;
using InventoryERP.Domain.Wms;
using InventoryERP.Infrastructure.Stocks;
using Xunit;

public sealed class R036_DefaultResolverTests
{
    private sealed class StubRepo : IWmsRepository
    {
        public (Warehouse wh, Location loc)? ProductDefault;
        public (Warehouse wh, Location loc)? SystemDefault;
        public (Warehouse wh, Location loc)? Unassigned;

        public Task<(Warehouse wh, Location loc)?> GetProductDefaultAsync(int productId) => Task.FromResult(ProductDefault);
        public Task<(Warehouse wh, Location loc)?> GetSystemDefaultAsync() => Task.FromResult(SystemDefault);
        public Task<(Warehouse wh, Location loc)?> GetUnassignedAsync() => Task.FromResult(Unassigned);
    }

    [Fact]
    public async Task Uses_Product_Default_When_Available()
    {
        var repo = new StubRepo
        {
            ProductDefault = (new Warehouse{Id=1,Code="W1",Name="W1",IsDefault=false,IsActive=true}, new Location{Id=10,WarehouseId=1,Code="L1",Name="L1",IsDefault=false,IsActive=true})
        };
        var resolver = new DefaultResolver(() => repo);
        var (wh, loc) = await resolver.ResolveAsync(100);
        Assert.Equal(1, wh.Id);
        Assert.Equal(10, loc.Id);
    }

    [Fact]
    public async Task Falls_Back_To_System_Default()
    {
        var repo = new StubRepo
        {
            SystemDefault = (new Warehouse{Id=2,Code="MAIN",Name="MAIN",IsDefault=true,IsActive=true}, new Location{Id=20,WarehouseId=2,Code="DEFAULT",Name="DEFAULT",IsDefault=true,IsActive=true})
        };
        var resolver = new DefaultResolver(() => repo);
        var (wh, loc) = await resolver.ResolveAsync(null);
        Assert.Equal(2, wh.Id);
        Assert.Equal("DEFAULT", loc.Code);
    }

    [Fact]
    public async Task Returns_Unassigned_When_No_Defaults()
    {
        var repo = new StubRepo
        {
            Unassigned = (new Warehouse{Id=3,Code="MAIN",Name="MAIN",IsDefault=true,IsActive=true}, new Location{Id=30,WarehouseId=3,Code="UNASSIGNED",Name="UNASSIGNED",IsDefault=false,IsActive=true,VisibleInUI=false})
        };
        var resolver = new DefaultResolver(() => repo);
        var (wh, loc) = await resolver.ResolveAsync(null);
        Assert.Equal(30, loc.Id);
        Assert.Equal("UNASSIGNED", loc.Code);
    }
}


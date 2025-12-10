using System.Threading.Tasks;
using InventoryERP.Infrastructure.Services;
using Xunit;

namespace Tests.Integration;

public class ReportingServiceTests
{
    [Fact]
    public async Task GenerateDummyPdf_Returns_NonEmpty_Bytes()
    {
        var svc = new ReportingService();
        var bytes = await svc.GenerateAsync("Test", "Content");
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 100);
    }
}

using System.Threading.Tasks;

namespace InventoryERP.Application.Reports;

public interface IReportingService
{
    Task<byte[]> GenerateAsync(string title, string content);
}

using System.Threading.Tasks;

namespace InventoryERP.Application.Import;

public interface IImportService
{
    Task<int> ImportProductsFromCsvAsync(string filePath);
    Task<int> ImportPartnersFromCsvAsync(string filePath);
    Task<int> ImportOpeningBalancesFromCsvAsync(string filePath);
}

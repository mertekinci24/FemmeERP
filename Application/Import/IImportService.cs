using System.Threading.Tasks;

namespace InventoryERP.Application.Import;

public interface IImportService
{
    Task<ImportResult> ImportProductsFromCsvAsync(string filePath, bool safeMode = true);
    Task<int> ImportPartnersFromCsvAsync(string filePath);
    Task<int> ImportOpeningBalancesFromCsvAsync(string filePath);
}

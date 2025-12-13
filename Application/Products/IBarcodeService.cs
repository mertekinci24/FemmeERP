using System.Threading.Tasks;

namespace InventoryERP.Application.Products
{
    public interface IBarcodeService
    {
        Task<string> GenerateUniqueBarcodeAsync();
    }
}

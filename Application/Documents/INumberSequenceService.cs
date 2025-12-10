using System.Threading.Tasks;

namespace InventoryERP.Application.Documents
{
    public interface INumberSequenceService
    {
        Task<string> GenerateNextNumberAsync(string documentType);
    }
}

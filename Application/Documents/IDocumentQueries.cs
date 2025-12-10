// Deprecated scaffold: the paged IDocumentQueries was added during an in-progress refactor
// and conflicts with the canonical Contracts-based API (`Contracts.cs`).
// Keep this file for reference but exclude from compilation until a full migration is agreed.

#if false
using InventoryERP.Application.Documents.DTOs;
using System.Threading.Tasks;

namespace InventoryERP.Application.Documents
{
    public interface IDocumentQueries
    {
        Task<PagedResult<DocumentRowDto>> ListAsync(DocumentListFilter filter);
        Task<DocumentDetailDto?> GetAsync(int id);
    }
}
#endif

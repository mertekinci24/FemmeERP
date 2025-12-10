using InventoryERP.Application.Partners;
using System.Threading.Tasks;

namespace InventoryERP.Infrastructure.Services
{
    public class PartnerCommandService : IPartnerCommandService
    {
        public Task<int> CreateAsync(PartnerDetailDto dto)
        {
            // TODO: Implement actual data access
            return Task.FromResult(1); // Return dummy id
        }

        public Task UpdateAsync(PartnerDetailDto dto)
        {
            // TODO: Implement actual data access
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            // TODO: Implement actual data access
            return Task.CompletedTask;
        }
    }
}

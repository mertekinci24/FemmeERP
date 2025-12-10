using InventoryERP.Application.Reports;
using System.Threading.Tasks;

namespace InventoryERP.Presentation.ViewModels
{
    public sealed class DashboardViewModel : ViewModelBase
    {
        public string Title { get; } = "Dashboard";

        // Keep a reference to queries in case the VM grows responsibilities later
        private readonly InventoryERP.Application.Reports.IDashboardQueries _queries;

        public DashboardViewModel(InventoryERP.Application.Reports.IDashboardQueries queries)
        {
            _queries = queries;
        }

        // Placeholder for future async initialization
        public Task InitializeAsync() => Task.CompletedTask;
    }
}




namespace InventoryERP.Presentation.Common;

using System.Threading.Tasks;

public interface IAutoRefresh
{
    Task RefreshAsync();
}


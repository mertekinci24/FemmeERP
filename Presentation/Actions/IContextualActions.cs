using System.Windows.Input;

namespace InventoryERP.Presentation.Actions
{
    /// <summary>
    /// Optional interface for ViewModels that expose contextual actions
    /// (New, Export, Filters preview) which the Shell can surface.
    /// Properties may be null if the action is not applicable.
    /// </summary>
    public interface IContextualActions
    {
        ICommand? NewCommand { get; }
        ICommand? ExportCommand { get; }
        ICommand? FiltersPreviewCommand { get; }
    }
}

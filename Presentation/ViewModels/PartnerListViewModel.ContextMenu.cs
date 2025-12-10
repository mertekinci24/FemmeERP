// R-150/R-151: Dynamic context menu population for PartnerListView
#nullable enable
using System.Collections.ObjectModel;

namespace InventoryERP.Presentation.ViewModels;

public sealed partial class PartnerListViewModel
{
    /// <summary>
    /// Build context menu items dynamically based on current selection/state.
    /// </summary>
    public void RebuildContextMenuItems()
    {
        DynamicContextMenuItems.Clear();

        var partner = SelectedPartner;
        // R-155: Always show menu items; commands will determine CanExecute
        DynamicContextMenuItems.Add(new MenuItemViewModel("ğŸ“Š Hareketler", OpenLedgerCommand, partner));
        DynamicContextMenuItems.Add(new MenuItemViewModel("ğŸ“Š Excel'e Aktar (.xlsx)", ExportListToExcelCommand, null));
        DynamicContextMenuItems.Add(new MenuItemViewModel("ğŸ“„ PDF'e Aktar (.pdf)", ExportListToPdfCommand, null));
    }
}

public sealed record MenuItemViewModel(string Header, System.Windows.Input.ICommand Command, object? CommandParameter);


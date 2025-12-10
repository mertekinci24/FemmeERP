// ReSharper disable once All
#nullable enable
using System;
using System.Windows.Input;

namespace InventoryERP.Presentation.ViewModels
{
    /// <summary>
    /// R-045: Production (MRP) Module
    /// </summary>
    public class ProductionModuleViewModel : ViewModelBase, InventoryERP.Presentation.Actions.IContextualActions
    {
        // R-045: Contextual commands for Production module
        public ICommand NewProductionOrderCmd { get; }
        public ICommand NewBomCmd { get; }

        public ProductionModuleViewModel()
        {
            // Initialize commands
            NewProductionOrderCmd = new Commands.RelayCommand(_ => NewProductionOrder());
            NewBomCmd = new Commands.RelayCommand(_ => NewBom());
        }

        private void NewProductionOrder()
        {
            System.Windows.MessageBox.Show("Yeni Ãœretim FiÅŸi (TODO - R-014)", "Bilgi", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void NewBom()
        {
            System.Windows.MessageBox.Show("Yeni ReÃ§ete (TODO)", "Bilgi", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        // IContextualActions implementation
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.NewCommand => NewProductionOrderCmd;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.ExportCommand => null;
        ICommand? InventoryERP.Presentation.Actions.IContextualActions.FiltersPreviewCommand => null;
    }
}


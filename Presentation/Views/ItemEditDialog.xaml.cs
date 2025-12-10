using System.Windows;
using InventoryERP.Presentation.ViewModels;

namespace InventoryERP.Presentation.Views
{
	/// <summary>
	/// R-040: Item Edit Dialog - Create/Edit Product with Multi-UOM and Warehouse Stock
	/// </summary>
	public partial class ItemEditDialog : Window
	{
		private readonly ItemEditViewModel _viewModel;

		public ItemEditDialog(ItemEditViewModel viewModel)
		{
			InitializeComponent();
			_viewModel = viewModel;
			DataContext = viewModel;

			// Monitor DialogResult property to close window
			_viewModel.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(ItemEditViewModel.DialogResult))
				{
					DialogResult = _viewModel.DialogResult;
					Close();
				}
			};
		}
	}
}

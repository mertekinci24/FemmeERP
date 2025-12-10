using System.Windows.Controls;
using System.Windows;
using InventoryERP.Application.Reports;
using InventoryERP.Presentation.Views.Controls;

namespace InventoryERP.Presentation.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView(IDashboardQueries queries)
        {
            InitializeComponent();

            // Instantiate widgets using DI-provided queries so each widget can create its own ViewModel
            var w00 = new ThisMonthSalesWidget(queries);
            var w01 = new OverdueReceivablesWidget(queries);
            var w11 = new TopProductsWidget(queries);

            // Add them into placeholders
            if (Widget00 != null) Widget00.Child = w00;
            if (Widget01 != null) Widget01.Child = w01;
            if (Widget11 != null) Widget11.Child = w11;
        }
    }
}



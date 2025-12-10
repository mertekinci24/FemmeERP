// Helper proxy to allow binding to DataContext within detached elements (e.g., ContextMenu)
#nullable enable
using System.Windows;

namespace InventoryERP.Presentation.Common
{
    public class BindingProxy : Freezable
    {
        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }
    }
}

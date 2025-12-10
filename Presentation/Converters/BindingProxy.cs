using System.Windows;

namespace InventoryERP.Presentation.Converters
{
    /// <summary>
    /// A Freezable-based proxy to allow DataGrid columns (which are not in the Visual Tree)
    /// to bind to the Window's DataContext.
    /// Usage: Add to Window.Resources as <converters:BindingProxy x:Key="Proxy" Data="{Binding}"/>
    /// Then bind columns via Source={StaticResource Proxy}, Path=Data.PropertyName
    /// </summary>
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }
}

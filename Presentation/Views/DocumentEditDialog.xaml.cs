using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Application.Products;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.Views;

public partial class DocumentEditDialog : Window
{
    private readonly DocumentEditViewModel _vm;
    private readonly IServiceScope _scope;
    private bool _isSaving;
    private bool _isClosing;
    public static readonly RoutedUICommand CloseCommand = new("Close", "Close", typeof(DocumentEditDialog));

    public DocumentEditDialog(DocumentEditViewModel viewModel, IServiceScope scope)
    {
        InitializeComponent();
        _scope = scope;
        _vm = viewModel;
        DataContext = _vm;
        _vm.SaveCompleted += ok => { if (ok) { DialogResult = true; Close(); } };
        _vm.CancelRequested += () => { DialogResult = false; Close(); };
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        try
        {
            _scope.Dispose();
        }
        catch
        {
            // ignore disposal errors
        }
    }

    private async Task<bool> SaveInternalAsync()
    {
        if (_isSaving) return false;
        _isSaving = true;
        // Validate against current UI state (Lines), not the DTO snapshot which is only updated during Save
        if (_vm.Lines == null || _vm.Lines.Count == 0)
        {
            MessageBox.Show("Document must have at least one line.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            _isSaving = false;
            return false;
        }
        var ok = await _vm.SaveAsync();
        if (ok)
        {
            DialogResult = true;
            Close();
            _isSaving = false;
            return true;
        }
        else
        {
            var errors = string.Join("\n", _vm.GetErrors(null));
            MessageBox.Show(errors.Length > 0 ? errors : "Validation failed.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            _isSaving = false;
            return false;
        }
    }

    private async void OnSaveClicked(object sender, ExecutedRoutedEventArgs e)
    {
        await SaveInternalAsync();
    }

    private void OnComboBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is System.Windows.Controls.ComboBox combo)
        {
            var line = combo.DataContext as InventoryERP.Presentation.ViewModels.DocumentEditViewModel.LineViewModel;
            if (e.Key == Key.Enter)
            {
                // try to commit selection
                if (combo.SelectedItem is InventoryERP.Application.Products.ProductRowDto prod)
                {
                    if (line != null)
                    {
                        // R-065: Only set ItemId - LineViewModel_PropertyChanged will auto-fill ItemName, VatRate, etc.
                        // Removing redundant ItemName set to avoid race conditions with async PropertyChanged handler
                        line.ItemId = prod.Id;
                    }
                }
                else
                {
                    // try find first matching item by text
                    var text = combo.Text ?? string.Empty;
                    var vm = DataContext as InventoryERP.Presentation.ViewModels.DocumentEditViewModel;
                    var found = vm?.Products.FirstOrDefault(p => (p.Name ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 || (p.Sku ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (found != null && line != null)
                    {
                        // R-065: Only set ItemId - LineViewModel_PropertyChanged will auto-fill ItemName, VatRate, etc.
                        line.ItemId = found.Id;
                    }
                }
                e.Handled = true;
                // move focus to Qty cell and begin edit
                if (line != null) MoveFocusToQty(line);
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (combo.Items.Count > 0)
                {
                    combo.SelectedIndex = Math.Max(0, combo.SelectedIndex - 1);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Down)
            {
                if (combo.Items.Count > 0)
                {
                    combo.SelectedIndex = Math.Min(combo.Items.Count - 1, combo.SelectedIndex + 1);
                    e.Handled = true;
                }
            }
        }
    }

    private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is InventoryERP.Application.Products.ProductRowDto prod)
        {
            if (cb.DataContext is InventoryERP.Presentation.ViewModels.DocumentEditViewModel.LineViewModel line)
            {
                // Mouse selection scenario (no Enter key pressed)
                line.ItemId = prod.Id;
                MoveFocusToQty(line);
            }
        }
    }

    private void MoveFocusToQty(InventoryERP.Presentation.ViewModels.DocumentEditViewModel.LineViewModel lineVm)
    {
        if (LinesGrid == null) return;

        var rowIndex = LinesGrid.Items.IndexOf(lineVm);
        if (rowIndex < 0) return;

        // Find Qty column by SortMemberPath or Header
        var qtyCol = LinesGrid.Columns.FirstOrDefault(c => string.Equals(c.SortMemberPath, "Qty", StringComparison.Ordinal) || string.Equals(c.Header?.ToString(), "Adet", StringComparison.OrdinalIgnoreCase) || string.Equals(c.Header?.ToString(), "Miktar", StringComparison.OrdinalIgnoreCase));
        if (qtyCol == null) return;

        LinesGrid.SelectedIndex = rowIndex;
        LinesGrid.ScrollIntoView(LinesGrid.Items[rowIndex]);
        LinesGrid.CurrentCell = new DataGridCellInfo(LinesGrid.Items[rowIndex], qtyCol);

        // Begin editing the cell on the UI thread
        Dispatcher.BeginInvoke(new Action(() =>
        {
            LinesGrid.Focus();
            LinesGrid.BeginEdit();
        }), DispatcherPriority.Background);
    }

    private async void OnSaveExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        await SaveInternalAsync();
    }

    private void OnCancelClicked(object sender, ExecutedRoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnCancelExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnAddLineContextMenuClick(object sender, RoutedEventArgs e)
    {
        if (_vm?.AddLineCmd?.CanExecute(null) == true)
        {
            _vm.AddLineCmd.Execute(null);
        }
    }

    private DocumentEditViewModel.LineViewModel? GetLineFromMenuItem(object? sender)
    {
        if (sender is MenuItem menu && menu.Parent is ContextMenu ctx && ctx.PlacementTarget is FrameworkElement fe)
        {
            return fe.DataContext as DocumentEditViewModel.LineViewModel;
        }
        return null;
    }

    private void OnRowDeleteClick(object sender, RoutedEventArgs e)
    {
        var line = GetLineFromMenuItem(sender);
        if (line != null)
        {
            _vm.RemoveLineParamCmd.Execute(line);
        }
    }

    private void OnRowMoveUpClick(object sender, RoutedEventArgs e)
    {
        var line = GetLineFromMenuItem(sender);
        if (line != null)
        {
            _vm.MoveLineUpParamCmd.Execute(line);
        }
    }

    private void OnRowMoveDownClick(object sender, RoutedEventArgs e)
    {
        var line = GetLineFromMenuItem(sender);
        if (line != null)
        {
            _vm.MoveLineDownParamCmd.Execute(line);
        }
    }

    private void OnRowMoveTopClick(object sender, RoutedEventArgs e)
    {
        var line = GetLineFromMenuItem(sender);
        if (line != null)
        {
            _vm.MoveLineTopParamCmd.Execute(line);
        }
    }

    private void OnRowMoveBottomClick(object sender, RoutedEventArgs e)
    {
        var line = GetLineFromMenuItem(sender);
        if (line != null)
        {
            _vm.MoveLineBottomParamCmd.Execute(line);
        }
    }
    private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}





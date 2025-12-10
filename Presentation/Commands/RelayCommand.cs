// ReSharper disable once All
#nullable enable
using System;
using System.Windows.Input;

namespace InventoryERP.Presentation.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _exec;
        private readonly Func<object?, bool>? _can;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _exec = execute;
            _can = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _exec(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

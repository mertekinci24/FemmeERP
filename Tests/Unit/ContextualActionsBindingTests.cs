using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using InventoryERP.Presentation;
using InventoryERP.Presentation.Actions;

namespace Tests.Unit
{
    public class ContextualActionsBindingTests
    {
        class FakeActions : IContextualActions
        {
            public bool NewExecuted;
            public FakeActions()
            {
                NewExecuted = false;
                NewCommand = new RelayCommand(_ => NewExecuted = true);
                ExportCommand = null;
                FiltersPreviewCommand = null;
            }
            public System.Windows.Input.ICommand? NewCommand { get; }
            public System.Windows.Input.ICommand? ExportCommand { get; }
            public System.Windows.Input.ICommand? FiltersPreviewCommand { get; }
        }

        [Fact]
        public void ShellWiresContextualActionsFromViewModel()
        {
            var sc = new ServiceCollection();
            sc.AddSingleton<ShellViewModel>();
            var sp = sc.BuildServiceProvider();
            var shell = sp.GetRequiredService<ShellViewModel>();

            // Initially no contextual actions -> disabled
            shell.CurrentView = new object();
            Assert.False(shell.NewCmd.CanExecute(null));

            // When CurrentView is a VM implementing IContextualActions, Shell should wire it
            var fake = new FakeActions();
            shell.CurrentView = fake;
            Assert.Same(fake.NewCommand, shell.NewCmd);
            Assert.True(shell.NewCmd.CanExecute(null));

            // Executing should invoke underlying command
            fake.NewExecuted = false;
            shell.NewCmd.Execute(null);
            Assert.True(fake.NewExecuted);
        }
    }
}

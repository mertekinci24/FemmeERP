using System;
using InventoryERP.Application.Export;
using InventoryERP.Application.Import;
using InventoryERP.Application.Partners;
using InventoryERP.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Unit;

/// <summary>
/// TST-024 (R-148): Tests for PartnerListViewModel command initialization
/// </summary>
public class PartnerListViewModelTests : IDisposable
{
    private Microsoft.Data.Sqlite.SqliteConnection? _connection;

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    [WpfFact]
    public void OpenLedgerCommand_ShouldNotBeNull()
    {
        // Arrange: Create real DI container with in-memory database
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        // Act: Create PartnerListViewModel with required dependencies (mocking UI services)
        var vm = new PartnerListViewModel(
            provider.GetRequiredService<IPartnerService>(),
            provider.GetRequiredService<IPartnerReadService>(),
            new Mock<InventoryERP.Presentation.Abstractions.IDialogService>().Object,
            provider.GetRequiredService<IExcelImportService>(),
            new Mock<InventoryERP.Presentation.Abstractions.IFileDialogService>().Object,
            provider.GetRequiredService<IExcelExportService>(),
            provider.GetRequiredService<Application.Export.IListPdfExportService>(),
            provider.GetRequiredService<IPartnerExportService>());

        // Assert: Verify that OpenLedgerCommand is initialized (R-148)
        Assert.NotNull(vm.OpenLedgerCommand);
    }

    [WpfFact]
    public void OpenLedgerCommand_ShouldBeExecutableWithSelectedPartner()
    {
        // Arrange: Create real DI container with in-memory database
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = new PartnerListViewModel(
            provider.GetRequiredService<IPartnerService>(),
            provider.GetRequiredService<IPartnerReadService>(),
            new Mock<InventoryERP.Presentation.Abstractions.IDialogService>().Object,
            provider.GetRequiredService<IExcelImportService>(),
            new Mock<InventoryERP.Presentation.Abstractions.IFileDialogService>().Object,
            provider.GetRequiredService<IExcelExportService>(),
            provider.GetRequiredService<Application.Export.IListPdfExportService>(),
            provider.GetRequiredService<IPartnerExportService>());

        // Act: Simulate selecting a partner
        var dummyPartner = new PartnerRowDto
        {
            Id = 1,
            Name = "Test Partner"
        };

        // Assert: OpenLedgerCommand should be executable with a PartnerRowDto parameter
        Assert.True(vm.OpenLedgerCommand.CanExecute(dummyPartner), 
            "OpenLedgerCommand should be executable with a PartnerRowDto parameter");
    }

    [WpfFact]
    public void OpenStatementCommand_ShouldNotBeNull()
    {
        // Arrange: Create real DI container with in-memory database
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        // Act: Create PartnerListViewModel with required dependencies (mocking UI services)
        var vm = new PartnerListViewModel(
            provider.GetRequiredService<IPartnerService>(),
            provider.GetRequiredService<IPartnerReadService>(),
            new Mock<InventoryERP.Presentation.Abstractions.IDialogService>().Object,
            provider.GetRequiredService<IExcelImportService>(),
            new Mock<InventoryERP.Presentation.Abstractions.IFileDialogService>().Object,
            provider.GetRequiredService<IExcelExportService>(),
            provider.GetRequiredService<Application.Export.IListPdfExportService>(),
            provider.GetRequiredService<IPartnerExportService>());

        // Assert: Verify that OpenStatementCommand still exists (backward compatibility)
        Assert.NotNull(vm.OpenStatementCommand);
    }

    [WpfFact]
    public void TST_025_DynamicContextMenuItems_ShouldPopulate_OnSelection()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var vm = new PartnerListViewModel(
            provider.GetRequiredService<IPartnerService>(),
            provider.GetRequiredService<IPartnerReadService>(),
            new Mock<InventoryERP.Presentation.Abstractions.IDialogService>().Object,
            provider.GetRequiredService<IExcelImportService>(),
            new Mock<InventoryERP.Presentation.Abstractions.IFileDialogService>().Object,
            provider.GetRequiredService<IExcelExportService>(),
            provider.GetRequiredService<Application.Export.IListPdfExportService>(),
            provider.GetRequiredService<IPartnerExportService>());

        vm.SelectedPartner = new PartnerRowDto { Id = 1, Name = "Test Partner" };
        vm.RebuildContextMenuItems();

        Assert.True(vm.DynamicContextMenuItems.Count >= 3, "Dynamic context menu should include Hareketler + Excel/PDF export items.");
    }
}

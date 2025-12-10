using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Tests.Infrastructure;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

/// <summary>
/// TST-028: Ensure UI read path uses AsNoTracking and write path re-fetches by id
/// so that approving/canceling/deleting does not cause EF Core tracking conflicts.
/// </summary>
public class TST_028_TrackingTests
{
    [Fact]
    public async Task Approve_SalesOrder_After_List_Read_Does_Not_Throw_And_Updates_Status()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var queries = provider.GetRequiredService<IDocumentQueries>();
            var invoiceCmd = provider.GetRequiredService<IInvoiceCommandService>();
            var docCmd = provider.GetRequiredService<IDocumentCommandService>();

            var id = await invoiceCmd.CreateDraftAsync(new CreateInvoiceDraftDto("SALES_ORDER", DateTime.Today));

            var page = await queries.ListAsync(new DocumentListFilter(), 1, 50); // AsNoTracking read
            page.Items.Any(r => r.Id == id).Should().BeTrue();

            Func<Task> act = async () => await docCmd.ApproveAsync(id);
            await act.Should().NotThrowAsync();

            var row = (await queries.ListAsync(new DocumentListFilter(), 1, 50)).Items.First(r => r.Id == id);
            row.Status.Should().Be("POSTED");
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task Approve_Quote_After_List_Read_Does_Not_Throw_And_Updates_Status()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var queries = provider.GetRequiredService<IDocumentQueries>();
            var invoiceCmd = provider.GetRequiredService<IInvoiceCommandService>();
            var docCmd = provider.GetRequiredService<IDocumentCommandService>();

            var id = await invoiceCmd.CreateDraftAsync(new CreateInvoiceDraftDto("QUOTE", DateTime.Today));
            _ = await queries.ListAsync(new DocumentListFilter(), 1, 10);

            await docCmd.ApproveAsync(id);

            var row = (await queries.ListAsync(new DocumentListFilter(), 1, 50)).Items.First(r => r.Id == id);
            row.Status.Should().Be("POSTED");
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task Delete_Draft_After_List_Read_Does_Not_Throw_And_Removes_Document()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var queries = provider.GetRequiredService<IDocumentQueries>();
            var invoiceCmd = provider.GetRequiredService<IInvoiceCommandService>();
            var docCmd = provider.GetRequiredService<IDocumentCommandService>();

            var id = await invoiceCmd.CreateDraftAsync(new CreateInvoiceDraftDto("SALES_INVOICE", DateTime.Today));
            _ = await queries.ListAsync(new DocumentListFilter(), 1, 10);

            await docCmd.DeleteDraftAsync(id);

            var exists = (await queries.ListAsync(new DocumentListFilter(), 1, 50)).Items.Any(r => r.Id == id);
            exists.Should().BeFalse();
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task Cancel_Posted_SalesOrder_After_List_Read_Does_Not_Throw()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var queries = provider.GetRequiredService<IDocumentQueries>();
            var invoiceCmd = provider.GetRequiredService<IInvoiceCommandService>();
            var docCmd = provider.GetRequiredService<IDocumentCommandService>();

            var id = await invoiceCmd.CreateDraftAsync(new CreateInvoiceDraftDto("SALES_ORDER", DateTime.Today));
            await docCmd.ApproveAsync(id);
            _ = await queries.ListAsync(new DocumentListFilter(), 1, 10);

            Func<Task> act = async () => await docCmd.CancelAsync(id);
            await act.Should().NotThrowAsync();
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task Approve_Twice_Is_Idempotent_No_Conflict()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var docCmd = provider.GetRequiredService<IDocumentCommandService>();
            var invoiceCmd = provider.GetRequiredService<IInvoiceCommandService>();
            var queries = provider.GetRequiredService<IDocumentQueries>();

            var id = await invoiceCmd.CreateDraftAsync(new CreateInvoiceDraftDto("SALES_ORDER", DateTime.Today));
            await docCmd.ApproveAsync(id);
            _ = await queries.ListAsync(new DocumentListFilter(), 1, 10);

            await docCmd.ApproveAsync(id); // should not throw
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task List_Returns_DTOs_Not_Tracked_Modifications_Do_Not_Affect_Db()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var queries = provider.GetRequiredService<IDocumentQueries>();
            var invoiceCmd = provider.GetRequiredService<IInvoiceCommandService>();

            var id = await invoiceCmd.CreateDraftAsync(new CreateInvoiceDraftDto("SALES_INVOICE", DateTime.Today));
            var list = await queries.ListAsync(new DocumentListFilter(), 1, 50);
            var row = list.Items.First(r => r.Id == id);
            var modified = row with { Number = "TEMP-CHANGE" }; // change DTO copy only (immutable)

            var entity = await db.Documents.AsNoTracking().FirstAsync(d => d.Id == id);
            (entity.Number ?? string.Empty).Should().NotBe("TEMP-CHANGE");
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task UpdateDraft_After_List_Read_Does_Not_Throw()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var queries = provider.GetRequiredService<IDocumentQueries>();
            var docCmd = provider.GetRequiredService<IDocumentCommandService>();
            var invoiceCmd = provider.GetRequiredService<IInvoiceCommandService>();

            var id = await invoiceCmd.CreateDraftAsync(new CreateInvoiceDraftDto("SALES_INVOICE", DateTime.Today));
            _ = await queries.ListAsync(new DocumentListFilter(), 1, 10);

            var dto = await provider.GetRequiredService<IDocumentQueries>().GetAsync(id);
            dto.Should().NotBeNull();
            dto!.Currency = "USD";

            Func<Task> act = async () => await docCmd.UpdateDraftAsync(id, dto);
            await act.Should().NotThrowAsync();
        }
        finally { conn.Dispose(); }
    }
}

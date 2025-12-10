using System.Threading.Tasks;
using InventoryERP.Domain.Entities;
using InventoryERP.Infrastructure.Services;
using Persistence;
using Xunit;
using Microsoft.EntityFrameworkCore;

public class AllocationServiceTests
{
    [Fact]
    public async Task AllocateAsync_AddsAllocationAndValidatesRules()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "AllocateAsyncTest")
            .Options;
        using var db = new AppDbContext(options);
    var partner = new Partner { Title = "Test" };
        db.Partners.Add(partner);
        db.SaveChanges();
        var payment = new PartnerLedgerEntry {
            PartnerId = partner.Id,
            AmountTry = 100m,
            Status = Domain.Enums.LedgerStatus.OPEN
        };
        var invoice = new PartnerLedgerEntry {
            PartnerId = partner.Id,
            AmountTry = 50m,
            Status = Domain.Enums.LedgerStatus.OPEN
        };
        db.PartnerLedgerEntries.Add(payment);
        db.PartnerLedgerEntries.Add(invoice);
        db.SaveChanges();
        var service = new AllocationService(db);
        await service.AllocateAsync(payment.Id, invoice.Id, 50m);
        var alloc = await db.PaymentAllocations.SingleAsync(a => a.PaymentEntryId == payment.Id && a.InvoiceEntryId == invoice.Id);
        Assert.Equal(50m, alloc.AmountTry);
    }
}

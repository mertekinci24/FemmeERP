
using System.Threading.Tasks;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Services;
using Persistence;
using Tests.Infrastructure;
using Xunit;

public class CreditPolicyServiceTests : BaseIntegrationTest
{
    [Fact]
    public async Task NoLimit_AlwaysPasses()
    {
        var partner = new Partner { Title = "Test", Role = Domain.Enums.PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);
        Ctx.SaveChanges();
        var aging = new AgingService(Ctx);
        var policy = new CreditPolicyService(Ctx, aging);
        await policy.EnsureCreditAvailableAsync(partner.Id, 100m);
    }

    [Fact]
    public async Task Limit_Sufficient_Passes()
    {
        var partner = new Partner { Title = "Test", Role = Domain.Enums.PartnerRole.CUSTOMER, CreditLimitTry = 500m };
        Ctx.Partners.Add(partner);
        Ctx.SaveChanges();
            var doc = new Document { Type = DocumentType.SALES_INVOICE, Date = System.DateTime.UtcNow, Status = DocumentStatus.APPROVED, PartnerId = partner.Id, Currency = "TRY", TotalTry = 100 };
            Ctx.Documents.Add(doc);
            Ctx.SaveChanges();
            var entry = new PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, Date = System.DateTime.UtcNow, Debit = 100, Credit = 0, AmountTry = 100, Status = Domain.Enums.LedgerStatus.OPEN };
            Ctx.PartnerLedgerEntries.Add(entry);
            Ctx.SaveChanges();
        var aging = new AgingService(Ctx);
        var policy = new CreditPolicyService(Ctx, aging);
        await policy.EnsureCreditAvailableAsync(partner.Id, 100m);
    }

    [Fact]
    public async Task Limit_Exceeded_Throws()
    {
        var partner = new Partner { Title = "Test", Role = Domain.Enums.PartnerRole.CUSTOMER, CreditLimitTry = 100m };
        Ctx.Partners.Add(partner);
        Ctx.SaveChanges();
            var doc = new Document { Type = DocumentType.SALES_INVOICE, Date = System.DateTime.UtcNow, Status = DocumentStatus.APPROVED, PartnerId = partner.Id, Currency = "TRY", TotalTry = 100 };
            Ctx.Documents.Add(doc);
            Ctx.SaveChanges();
            var entry = new PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, Date = System.DateTime.UtcNow, Debit = 100, Credit = 0, AmountTry = 100, Status = Domain.Enums.LedgerStatus.OPEN };
            Ctx.PartnerLedgerEntries.Add(entry);
            Ctx.SaveChanges();
        var aging = new AgingService(Ctx);
        var policy = new CreditPolicyService(Ctx, aging);
        await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
            await policy.EnsureCreditAvailableAsync(partner.Id, 50m));
    }
}

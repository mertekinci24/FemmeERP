using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;

public sealed class CreditPolicyService {
    private readonly AppDbContext _db;
    private readonly AgingService _aging;
    public CreditPolicyService(AppDbContext db, AgingService aging) {
        _db = db;
        _aging = aging;
    }
    public async Task EnsureCreditAvailableAsync(int partnerId, decimal deltaTry, CancellationToken ct = default) {
        var partner = await _db.Partners.FindAsync(new object[] { partnerId }, ct) ?? throw new InvalidOperationException("PARTNER-404");
        if (partner.CreditLimitTry is null) return;
        var aging = await _aging.GetPartnerAgingAsync(partnerId, null, ct);
        var nextRisk = aging.Total + deltaTry;
        if (nextRisk > partner.CreditLimitTry.Value)
            throw new InvalidOperationException("CREDIT-LIMIT-EXCEEDED");
    }
}

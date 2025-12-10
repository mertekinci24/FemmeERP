using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Unit;

public class TST_031_DictionarySafetyTests
{
    [Fact]
    public void SafeToDictionary_Allows_Duplicate_Keys_Last_Wins()
    {
        // Arrange: duplicate key 10 appears twice
        var data = new List<KeyValuePair<int, string>>
        {
            new(10, "first"),
            new(10, "last")
        };

        // Reflect Infrastructure.Utils.LinqExtensions.SafeToDictionary<TSource,TKey,TValue>
        var infraAsm = typeof(global::InventoryERP.Infrastructure.DependencyInjection).Assembly;
        var extType = infraAsm.GetType("Infrastructure.Utils.LinqExtensions", throwOnError: true)!;
        var mi = extType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .First(m => m.Name == "SafeToDictionary" && m.GetGenericArguments().Length == 3);
        var g = mi.MakeGenericMethod(typeof(KeyValuePair<int, string>), typeof(int), typeof(string));

        // Build delegates for selectors
        Func<KeyValuePair<int, string>, int> keySel = kv => kv.Key;
        Func<KeyValuePair<int, string>, string> valSel = kv => kv.Value;

        // Act: invoke extension
        var dict = (Dictionary<int, string>)g.Invoke(null, new object?[] { data, keySel, valSel, null! })!;

        // Assert: no ArgumentException and last value kept
        dict.Should().ContainKey(10);
        dict[10].Should().Be("last");
    }

    [Fact]
    public async Task PartnerReadService_GetListAsync_Does_Not_Throw_With_Duplicate_PartnerLedgerEntries()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var db = provider.GetRequiredService<AppDbContext>();
            // Seed a partner and duplicate ledger entries for the same partner (idempotent in summary)
            var partner = new InventoryERP.Domain.Entities.Partner { Name = "ACME", PartnerType = InventoryERP.Domain.Enums.PartnerType.Customer, IsActive = true };
            db.Partners.Add(partner);
            await db.SaveChangesAsync();

            // Create a dummy document to satisfy FK_PartnerLedgerEntry_Document_DocId
            var doc = new InventoryERP.Domain.Entities.Document { Type = InventoryERP.Domain.Enums.DocumentType.SALES_INVOICE, Date = DateTime.Today, Status = InventoryERP.Domain.Enums.DocumentStatus.POSTED, Number = "DUMMY-001" };
            db.Documents.Add(doc);
            await db.SaveChangesAsync();

            db.PartnerLedgerEntries.Add(new InventoryERP.Domain.Entities.PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, Date = DateTime.UtcNow, Debit = 100m, Credit = 0m, AmountTry = 100m, Status = InventoryERP.Domain.Enums.LedgerStatus.OPEN });
            db.PartnerLedgerEntries.Add(new InventoryERP.Domain.Entities.PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, Date = DateTime.UtcNow.AddMinutes(1), Debit = 50m, Credit = 0m, AmountTry = 50m, Status = InventoryERP.Domain.Enums.LedgerStatus.OPEN });
            await db.SaveChangesAsync();

            var svc = provider.GetRequiredService<InventoryERP.Application.Partners.IPartnerReadService>();

            var act = async () => await svc.GetListAsync(null, 1, 10);
            await act.Should().NotThrowAsync();
        }
        finally { conn.Dispose(); }
    }
}

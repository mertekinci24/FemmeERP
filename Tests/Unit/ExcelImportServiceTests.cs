using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Import;
using InventoryERP.Application.Partners;
using ClosedXML.Excel;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Partners;
using InventoryERP.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Xunit;

namespace Tests.Unit;

/// <summary>
/// R-113: Duplicate prevention + identifier normalization tests for ExcelImportService.
/// Validates that importing duplicate VKN or TCKN rows yields a per-row error and does not create a second record.
/// </summary>
public class ExcelImportServiceTests
{
    private static AppDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static IExcelImportService CreateService(AppDbContext ctx)
    {
        IPartnerService partnerService = new PartnerService(ctx);
        return new ExcelImportService(partnerService);
    }

    private static string CreateWorkbook(Action<IXLWorksheet> fill)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"excel_import_test_{Guid.NewGuid():N}.xlsx");
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Partners");
        // R-119: Updated header with new columns
        ws.Cell(1, 1).Value = "Cari Adý";
        ws.Cell(1, 2).Value = "Cari Tipi";
        ws.Cell(1, 3).Value = "VKN";
        ws.Cell(1, 4).Value = "TCKN";
        ws.Cell(1, 5).Value = "Telefon";
        ws.Cell(1, 6).Value = "e-posta";
        ws.Cell(1, 7).Value = "Ýlgili Kiþi Adý";
        ws.Cell(1, 8).Value = "Açýklama";
        ws.Cell(1, 9).Value = "Vade";
        ws.Cell(1, 10).Value = "Ödeme Türü";
        ws.Cell(1, 11).Value = "Risk Durumu";
        ws.Cell(1, 12).Value = "Ýþ Yeri Adresi";
        ws.Cell(1, 13).Value = "Sevkiyat Adresi";
        fill(ws);
        wb.SaveAs(temp);
        return temp;
    }

    [Fact]
    public async Task ImportPartnersAsync_Duplicate_VKN_Second_Row_Fails()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            // Row 2 (first data row)
            ws.Cell(2, 1).Value = "Firma A";           // Name
            ws.Cell(2, 2).Value = "SUPPLIER";          // PartnerType
            ws.Cell(2, 3).Value = "2222222222";        // TaxId (valid VKN)
            ws.Cell(2, 4).Value = "";                  // NationalId

            // Row 3 duplicate VKN different name
            ws.Cell(3, 1).Value = "Firma B";
            ws.Cell(3, 2).Value = "SUPPLIER";
            ws.Cell(3, 3).Value = "2222222222";        // duplicate TaxId
            ws.Cell(3, 4).Value = "";
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Errors);
        Assert.Contains("VKN '2222222222'", result.Errors.First());
        // Database should have exactly one partner with that VKN
        var count = ctx.Partners.Count(p => p.TaxId == "2222222222");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ImportPartnersAsync_Duplicate_TCKN_Second_Row_Fails()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "Kiþi A";
            ws.Cell(2, 2).Value = "CUSTOMER";
            ws.Cell(2, 3).Value = "";                 // TaxId
            ws.Cell(2, 4).Value = "12345678901";       // NationalId (valid TCKN)

            ws.Cell(3, 1).Value = "Kiþi B";
            ws.Cell(3, 2).Value = "CUSTOMER";
            ws.Cell(3, 3).Value = "";
            ws.Cell(3, 4).Value = "12345678901";       // duplicate TCKN
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Errors);
        Assert.Contains("TCKN '12345678901'", result.Errors.First());
        var count = ctx.Partners.Count(p => p.NationalId == "12345678901");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ImportPartnersAsync_Formatted_VKN_Normalized_Prevents_Duplicate()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "Firma Formatlý";
            ws.Cell(2, 2).Value = "SUPPLIER";
            ws.Cell(2, 3).Value = "222-222-2222";     // formatted VKN should normalize to 2222222222
            ws.Cell(2, 4).Value = "";

            ws.Cell(3, 1).Value = "Firma Duplicated";
            ws.Cell(3, 2).Value = "SUPPLIER";
            ws.Cell(3, 3).Value = "2222222222";       // canonical form
            ws.Cell(3, 4).Value = "";
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.Equal(1, result.SuccessCount); // first row succeeds
        Assert.Equal(1, result.FailureCount); // second row duplicate
        Assert.Single(result.Errors);
        Assert.Contains("VKN '2222222222'", result.Errors.First());
        var count = ctx.Partners.Count(p => p.TaxId == "2222222222");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ImportPartnersAsync_Turkish_PartnerType_Musteri_Parsed()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "Türkçe Müþteri";  // Name
            ws.Cell(2, 2).Value = "Müþteri";          // Turkish: Müþteri
            ws.Cell(2, 3).Value = "1111111111";       // VKN
            ws.Cell(2, 4).Value = "";                 // TCKN
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.True(result.Success);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        var partner = ctx.Partners.Single(p => p.Name == "Türkçe Müþteri");
        Assert.Equal(PartnerType.Customer, partner.PartnerType);
    }

    [Fact]
    public async Task ImportPartnersAsync_Turkish_PartnerType_Satici_Parsed()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "Türkçe Satýcý";   // Name
            ws.Cell(2, 2).Value = "Satýcý";           // Turkish: Satýcý
            ws.Cell(2, 3).Value = "3333333333";       // VKN
            ws.Cell(2, 4).Value = "";                 // TCKN
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.True(result.Success);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        var partner = ctx.Partners.Single(p => p.Name == "Türkçe Satýcý");
        Assert.Equal(PartnerType.Supplier, partner.PartnerType);
    }

    [Fact]
    public async Task ImportPartnersAsync_PartnerType_BOTH_Parsed()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "Hem Müþteri Hem Satýcý";
            ws.Cell(2, 2).Value = "BOTH";             // BOTH
            ws.Cell(2, 3).Value = "4444444444";       // VKN
            ws.Cell(2, 4).Value = "";                 // TCKN
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.True(result.Success);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        var partner = ctx.Partners.Single(p => p.Name == "Hem Müþteri Hem Satýcý");
        Assert.Equal(PartnerType.Both, partner.PartnerType);
    }

    [Fact]
    public async Task ImportPartnersAsync_Payment_Term_1_Hafta_Parsed()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "Firma Vade 1 Hafta";
            ws.Cell(2, 2).Value = "SUPPLIER";
            ws.Cell(2, 3).Value = "5555555555";
            ws.Cell(2, 4).Value = "";
            ws.Cell(2, 9).Value = "1 hafta";          // Vade: 1 hafta = 7 days
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.True(result.Success);
        Assert.Equal(1, result.SuccessCount);
        var partner = ctx.Partners.Single(p => p.Name == "Firma Vade 1 Hafta");
        Assert.Equal(7, partner.PaymentTermDays);
    }

    [Fact]
    public async Task ImportPartnersAsync_Payment_Term_7_Gun_Parsed()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "Firma Vade 7 Gün";
            ws.Cell(2, 2).Value = "CUSTOMER";
            ws.Cell(2, 3).Value = "5555555556";
            ws.Cell(2, 4).Value = "";
            ws.Cell(2, 9).Value = "7 Gün";            // Vade: 7 Gün = 7 days
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.True(result.Success);
        Assert.Equal(1, result.SuccessCount);
        var partner = ctx.Partners.Single(p => p.Name == "Firma Vade 7 Gün");
        Assert.Equal(7, partner.PaymentTermDays);
    }

    [Fact]
    public async Task ImportPartnersAsync_Payment_Term_7_Number_Parsed()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "Firma Vade 7 Sayý";
            ws.Cell(2, 2).Value = "CUSTOMER";
            ws.Cell(2, 3).Value = "5555555557";
            ws.Cell(2, 4).Value = "";
            ws.Cell(2, 9).Value = "7";                // Vade: 7 = 7 days
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.True(result.Success);
        Assert.Equal(1, result.SuccessCount);
        var partner = ctx.Partners.Single(p => p.Name == "Firma Vade 7 Sayý");
        Assert.Equal(7, partner.PaymentTermDays);
    }

    [Fact]
    public async Task ImportPartnersAsync_Payment_Term_30_Gun_Parsed()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "Firma Vade 30 Gün";
            ws.Cell(2, 2).Value = "CUSTOMER";
            ws.Cell(2, 3).Value = "6666666666";
            ws.Cell(2, 4).Value = "";
            ws.Cell(2, 9).Value = "30 gün";           // Vade: 30 gün
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.True(result.Success);
        Assert.Equal(1, result.SuccessCount);
        var partner = ctx.Partners.Single(p => p.Name == "Firma Vade 30 Gün");
        Assert.Equal(30, partner.PaymentTermDays);
    }

    [Fact]
    public async Task ImportPartnersAsync_All_New_Fields_Imported()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var file = CreateWorkbook(ws =>
        {
            ws.Cell(2, 1).Value = "Tam Veri Firmasý";
            ws.Cell(2, 2).Value = "Müþteri";
            ws.Cell(2, 3).Value = "7777777777";                     // VKN
            ws.Cell(2, 4).Value = "";                                // TCKN
            ws.Cell(2, 5).Value = "0532 123 45 67";                 // Telefon
            ws.Cell(2, 6).Value = "info@firma.com";                 // e-posta
            ws.Cell(2, 7).Value = "Ahmet Yýlmaz";                   // Ýlgili Kiþi
            ws.Cell(2, 8).Value = "Test açýklama";                  // Açýklama
            ws.Cell(2, 9).Value = "45 gün";                         // Vade
            ws.Cell(2, 10).Value = "Kredi Kartý";                   // Ödeme Türü
            ws.Cell(2, 11).Value = 100000;                          // Risk Durumu
            ws.Cell(2, 12).Value = "Ankara Ofis Adresi";            // Ýþ Yeri Adresi
            ws.Cell(2, 13).Value = "Ýstanbul Depo Adresi";          // Sevkiyat Adresi
        });

        var result = await service.ImportPartnersAsync(file);

        Assert.True(result.Success);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        
    var partner = ctx.Partners.Single(p => p.Name == "Tam Veri Firmasý");
    Assert.Equal(PartnerType.Customer, partner.PartnerType);
    Assert.Equal("7777777777", partner.TaxId);
    Assert.Equal(45, partner.PaymentTermDays);
    Assert.Equal(100000, partner.CreditLimitTry);
    }
}

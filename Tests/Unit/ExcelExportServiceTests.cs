using System;
using System.Collections.Generic;
using System.IO;
using InventoryERP.Application.Export;
using InventoryERP.Application.Partners;
using ClosedXML.Excel;
using InventoryERP.Infrastructure.Services;
using Xunit;

namespace Tests.Unit;

/// <summary>
/// TST-017: Tests for ExcelExportService (R-095)
/// Validates Excel list export with Turkish headers and proper data formatting
/// </summary>
public class ExcelExportServiceTests : IDisposable
{
    // R-122 FIX 1: Configure QuestPDF license for tests (R-109 lesson)
    static ExcelExportServiceTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                try { File.Delete(file); }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }

    private string GetTempFilePath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"excel_export_test_{Guid.NewGuid():N}.xlsx");
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public void ExportToExcel_WithPartnerData_CreatesValidExcelFile()
    {
        // Arrange
        IExcelExportService service = new ExcelExportService();
        var data = new List<PartnerCrudListDto>
        {
            new(1, "Test Firma A", "Customer", "1234567890", null, true),
            new(2, "Test Firma B", "Supplier", "9876543210", "12345678901", false)
        };
        var filePath = GetTempFilePath();

        // Act
        service.ExportToExcel(data, filePath, "Cari Listesi");

        // Assert
        Assert.True(File.Exists(filePath), "Excel file should be created");
        
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet("Cari Listesi");
        Assert.NotNull(worksheet);

    // Verify headers (Turkish translations) - after R-135 revert reduced to core fields
    Assert.Equal("Cari Adý", worksheet.Cell(1, 1).GetString());
    Assert.Equal("Cari Tipi", worksheet.Cell(1, 2).GetString());
    Assert.Equal("VKN", worksheet.Cell(1, 3).GetString());
    Assert.Equal("TCKN", worksheet.Cell(1, 4).GetString());
    Assert.Equal("Aktif", worksheet.Cell(1, 5).GetString());

        // Verify first row data
    Assert.Equal("Test Firma A", worksheet.Cell(2, 1).GetString());
    Assert.Equal("Customer", worksheet.Cell(2, 2).GetString());
    Assert.Equal("1234567890", worksheet.Cell(2, 3).GetString());
    Assert.Equal("", worksheet.Cell(2, 4).GetString());
    Assert.True(worksheet.Cell(2, 5).GetBoolean());

        // Verify second row data
    Assert.Equal("Test Firma B", worksheet.Cell(3, 1).GetString());
    Assert.Equal("Supplier", worksheet.Cell(3, 2).GetString());
    Assert.Equal("9876543210", worksheet.Cell(3, 3).GetString());
    Assert.Equal("12345678901", worksheet.Cell(3, 4).GetString());
    Assert.False(worksheet.Cell(3, 5).GetBoolean());
    }

    [Fact]
    public void ExportToExcel_WithEmptyData_CreatesExcelWithHeadersOnly()
    {
        // Arrange
        IExcelExportService service = new ExcelExportService();
        var data = new List<PartnerCrudListDto>();
        var filePath = GetTempFilePath();

        // Act
        service.ExportToExcel(data, filePath, "Empty");

        // Assert
        Assert.True(File.Exists(filePath), "Excel file should be created even with empty data");
        
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet("Empty");
        Assert.NotNull(worksheet);

        // R-122 FIX 2: Should have headers in first row
        Assert.Equal("Cari Adý", worksheet.Cell(1, 1).GetString());
        
        // R-122 FIX 2: Should have "Veri bulunamadý" message in second row
        Assert.Equal("Veri bulunamadý", worksheet.Cell(2, 1).GetString());
    }

    [Fact]
    public void ExportToExcel_WithInvalidPath_ThrowsException()
    {
        // Arrange
        IExcelExportService service = new ExcelExportService();
        var data = new List<PartnerCrudListDto>
        {
            new(1, "Test", "Customer", null, null, true)
        };
        var invalidPath = "Z:\\InvalidDrive\\Invalid\\Path\\test.xlsx";

        // R-122 FIX 4: Expect InvalidOperationException wrapper (not DirectoryNotFoundException)
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            service.ExportToExcel(data, invalidPath));
    }

    [Fact]
    public void ExportToExcel_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        IExcelExportService service = new ExcelExportService();
        var filePath = GetTempFilePath();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            service.ExportToExcel<PartnerCrudListDto>(null!, filePath));
    }

    [Fact]
    public void ExportToExcel_WithNullOrEmptyFilePath_ThrowsArgumentException()
    {
        // Arrange
        IExcelExportService service = new ExcelExportService();
        var data = new List<PartnerCrudListDto>
        {
            new(1, "Test", "Customer", null, null, true)
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            service.ExportToExcel(data, null!));
        Assert.Throws<ArgumentException>(() => 
            service.ExportToExcel(data, string.Empty));
    }
}

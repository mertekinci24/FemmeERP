using System;
using System.Collections.Generic;
using System.IO;
using InventoryERP.Application.Export;
using InventoryERP.Application.Partners;
using InventoryERP.Infrastructure.Services;
using Xunit;

namespace Tests.Unit;

/// <summary>
/// TST-018: Tests for ListPdfExportService (R-095)
/// Validates PDF list export with Turkish headers and table layout
/// </summary>
public class ListPdfExportServiceTests : IDisposable
{
    // R-122 FIX 1: Configure QuestPDF license for tests (R-109 lesson)
    static ListPdfExportServiceTests()
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
        var path = Path.Combine(Path.GetTempPath(), $"pdf_export_test_{Guid.NewGuid():N}.pdf");
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public void ExportToPdf_WithPartnerData_CreatesValidPdfFile()
    {
        // Arrange
        IListPdfExportService service = new ListPdfExportService();
        var data = new List<PartnerCrudListDto>
        {
            new(1, "Test Firma A", "Customer", "1234567890", null, true),
            new(2, "Test Firma B", "Supplier", "9876543210", "12345678901", false)
        };
        var filePath = GetTempFilePath();

        // Act
        service.ExportToPdf(data, filePath, "Cari Listesi");

        // Assert
        Assert.True(File.Exists(filePath), "PDF file should be created");
        
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 0, "PDF file should have content");
        
        // Verify PDF magic bytes (PDF header starts with %PDF)
        using var fs = File.OpenRead(filePath);
        var header = new byte[4];
        fs.Read(header, 0, 4);
        Assert.Equal(0x25, header[0]); // %
        Assert.Equal(0x50, header[1]); // P
        Assert.Equal(0x44, header[2]); // D
        Assert.Equal(0x46, header[3]); // F
    }

    [Fact]
    public void ExportToPdf_WithEmptyData_CreatesValidPdfWithHeadersOnly()
    {
        // Arrange
        IListPdfExportService service = new ListPdfExportService();
        var data = new List<PartnerCrudListDto>();
        var filePath = GetTempFilePath();

        // Act
        service.ExportToPdf(data, filePath, "Boþ Liste");

        // Assert
        Assert.True(File.Exists(filePath), "PDF file should be created even with empty data");
        
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 0, "PDF file should have content (at least headers and title)");
    }

    [Fact]
    public void ExportToPdf_WithInvalidPath_ThrowsException()
    {
        // Arrange
        IListPdfExportService service = new ListPdfExportService();
        var data = new List<PartnerCrudListDto>
        {
            new(1, "Test", "Customer", null, null, true)
        };
        var invalidPath = "Z:\\InvalidDrive\\Invalid\\Path\\test.pdf";

        // R-122 FIX 4: Expect InvalidOperationException wrapper (not DirectoryNotFoundException)
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            service.ExportToPdf(data, invalidPath, "Test"));
    }

    [Fact]
    public void ExportToPdf_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        IListPdfExportService service = new ListPdfExportService();
        var filePath = GetTempFilePath();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            service.ExportToPdf<PartnerCrudListDto>(null!, filePath, "Test"));
    }

    [Fact]
    public void ExportToPdf_WithNullOrEmptyFilePath_ThrowsArgumentException()
    {
        // Arrange
        IListPdfExportService service = new ListPdfExportService();
        var data = new List<PartnerCrudListDto>
        {
            new(1, "Test", "Customer", null, null, true)
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            service.ExportToPdf(data, null!, "Test"));
        Assert.Throws<ArgumentException>(() => 
            service.ExportToPdf(data, string.Empty, "Test"));
    }

    [Fact]
    public void ExportToPdf_WithLongDataSet_CreatesMultiPagePdf()
    {
        // Arrange
        IListPdfExportService service = new ListPdfExportService();
        var data = new List<PartnerCrudListDto>();
        
        // Create 50 partners to trigger multi-page layout
        for (int i = 1; i <= 50; i++)
        {
            data.Add(new PartnerCrudListDto(
                i,
                $"Test Firma {i}",
                i % 2 == 0 ? "Customer" : "Supplier",
                $"{1000000000 + i}",
                null,
                i % 3 != 0
            ));
        }
        var filePath = GetTempFilePath();

        // Act
        service.ExportToPdf(data, filePath, "Büyük Cari Listesi");

        // Assert
        Assert.True(File.Exists(filePath), "PDF file should be created");
        
        var fileInfo = new FileInfo(filePath);
        // Multi-page PDF should be significantly larger than single-page
        Assert.True(fileInfo.Length > 5000, "Multi-page PDF should be larger than 5KB");
    }
}

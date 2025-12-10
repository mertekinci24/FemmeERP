using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryERP.Application.Import;

/// <summary>
/// R-093: Service for importing partners (cari) from Excel files
/// </summary>
public interface IExcelImportService
{
    /// <summary>
    /// Imports partners from an Excel file
    /// </summary>
    /// <param name="filePath">Path to the Excel file (.xlsx)</param>
    /// <returns>Result indicating success and number of records imported/failed</returns>
    Task<ExcelImportResult> ImportPartnersAsync(string filePath);
}

/// <summary>
/// R-093: Result of Excel import operation
/// </summary>
public record ExcelImportResult
{
    public bool Success { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<string> Errors { get; init; } = new();
    public string Summary => Success 
        ? $"{SuccessCount} kayıt başarıyla içeri aktarıldı." 
        : $"{SuccessCount} başarılı, {FailureCount} hata. Detaylar: {string.Join("; ", Errors.Take(5))}";
}

using System.Collections.Generic;

namespace InventoryERP.Application.Export;

/// <summary>
/// R-095: Excel export service for list data
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Export a collection of data to an Excel (.xlsx) file
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    /// <param name="data">Collection of data to export</param>
    /// <param name="filePath">Target file path (must end with .xlsx)</param>
    /// <param name="sheetName">Excel sheet name (default: "Data")</param>
    void ExportToExcel<T>(IEnumerable<T> data, string filePath, string sheetName = "Data") where T : class;
}

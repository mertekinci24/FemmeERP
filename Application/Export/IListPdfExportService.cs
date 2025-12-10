using System.Collections.Generic;

namespace InventoryERP.Application.Export;

/// <summary>
/// R-095: PDF list export service for grid-based data
/// </summary>
public interface IListPdfExportService
{
    /// <summary>
    /// Export a collection of data to a PDF file with table layout
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    /// <param name="data">Collection of data to export</param>
    /// <param name="filePath">Target file path (must end with .pdf)</param>
    /// <param name="title">Document title</param>
    void ExportToPdf<T>(IEnumerable<T> data, string filePath, string title = "Liste") where T : class;
}

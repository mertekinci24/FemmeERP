using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryERP.Application.Stocks;

// R-279: Added PartnerName to trace source/destination
public sealed record StockMoveRowDto(
    DateTime Date, 
    string DocType, 
    string DocNo, 
    string? PartnerName, // NEW: Business Partner
    decimal Qty, 
    decimal? UnitCost, 
    string? Ref);

public interface IStockQueries
{
    Task<IReadOnlyList<StockMoveRowDto>> ListMovesAsync(int productId, DateOnly? from, DateOnly? to);
}

public interface IStockExportService
{
    Task<byte[]> ExportMovesExcelAsync(int productId, DateOnly? from, DateOnly? to);
}

using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using ClosedXML.Excel;
using InventoryERP.Application.Stocks;
using Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryERP.Infrastructure.Services
{
    public class StockExportServiceClosedXml : IStockExportService
    {
        private readonly AppDbContext _db;
        public StockExportServiceClosedXml(AppDbContext db) => _db = db;

        public async Task<byte[]> ExportMovesExcelAsync(int productId, DateOnly? from, DateOnly? to)
        {
            var q = _db.StockMoves.Include(s => s.DocumentLine).ThenInclude(dl => dl.Document).AsNoTracking().Where(s => s.ItemId == productId);
            if (from is not null) q = q.Where(s => s.Date >= from.Value.ToDateTime(new TimeOnly(0,0)));
            if (to is not null) q = q.Where(s => s.Date <= to.Value.ToDateTime(new TimeOnly(23,59,59)));

            #pragma warning disable CS8602
            var rows = await q.OrderByDescending(s => s.Date).Select(s => new
            {
                s.Date,
                DocType = s.DocumentLine!.Document!.Type.ToString() ?? string.Empty,
                DocNo = s.DocumentLine!.Document!.Number ?? string.Empty,
                Qty = s.QtySigned,
                UnitCost = s.UnitCost,
                Ref = s.Note
            }).ToListAsync();
            #pragma warning restore CS8602

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Hareketler");
            ws.Cell(1,1).Value = "Tarih";
            ws.Cell(1,2).Value = "Belge Türü";
            ws.Cell(1,3).Value = "Belge No";
            ws.Cell(1,4).Value = "Miktar";
            ws.Cell(1,5).Value = "Birim Maliyet";
            ws.Cell(1,6).Value = "Açıklama";

            var r = 2;
            foreach (var x in rows)
            {
                ws.Cell(r,1).Value = x.Date;
                ws.Cell(r,2).Value = x.DocType;
                ws.Cell(r,3).Value = x.DocNo;
                ws.Cell(r,4).Value = x.Qty;
                ws.Cell(r,5).Value = x.UnitCost;
                ws.Cell(r,6).Value = x.Ref;
                r++;
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}

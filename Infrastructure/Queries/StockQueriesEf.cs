using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using InventoryERP.Application.Stocks;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Queries
{
    public sealed class StockQueriesEf : IStockQueries
    {
        private readonly AppDbContext _db;
        public StockQueriesEf(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<StockMoveRowDto>> ListMovesAsync(int productId, DateOnly? from, DateOnly? to)
        {
            var q = _db.StockMoves
                .Include(s => s.Item)
                .Include(s => s.DocumentLine).ThenInclude(dl => dl.Document).ThenInclude(d => d.Partner) // R-279: Join Partner
                .AsNoTracking()
                .Where(s => s.ItemId == productId);

            if (from is not null)
            {
                var dt = from.Value.ToDateTime(new TimeOnly(0,0));
                q = q.Where(s => s.Date >= dt);
            }
            if (to is not null)
            {
                var dt = to.Value.ToDateTime(new TimeOnly(23,59,59));
                q = q.Where(s => s.Date <= dt);
            }

            #pragma warning disable CS8602
            var list = await q.OrderByDescending(s => s.Date).Select(s => new StockMoveRowDto(
                s.Date,
                s.DocumentLine!.Document!.Type.ToString() ?? string.Empty,
                s.DocumentLine!.Document!.Number ?? string.Empty,
                // R-279: Map Partner Name or fallback to Description
                s.DocumentLine!.Document!.Partner != null 
                    ? s.DocumentLine.Document.Partner.Name 
                    : (s.DocumentLine.Document.Description ?? ""),
                s.QtySigned,
                s.UnitCost,
                s.Note
            )).ToListAsync();
            #pragma warning restore CS8602

            return list;
        }
    }
}

using MediatR;
using Persistence;
using Microsoft.EntityFrameworkCore;
using InventoryERP.Infrastructure.CQRS.Queries;

namespace InventoryERP.Infrastructure.CQRS.Handlers;

public class GetThisMonthSalesHandler(AppDbContext db) : IRequestHandler<GetThisMonthSalesQuery, ThisMonthSalesDto>
{
    public async Task<ThisMonthSalesDto> Handle(GetThisMonthSalesQuery query, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var year = query.Year ?? now.Year;
        var month = query.Month ?? now.Month;
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddTicks(-1);

        var q = db.Documents.AsNoTracking()
            .Where(d => d.Type == InventoryERP.Domain.Enums.DocumentType.SALES_INVOICE && d.Status == InventoryERP.Domain.Enums.DocumentStatus.POSTED && d.Date >= start && d.Date <= end);

        // SQLite provider in-memory used in tests cannot translate SUM(decimal) aggregates reliably.
        // Execute query on server, then aggregate in memory to avoid NotSupportedException.
        var list = await q.Select(d => d.TotalTry).ToListAsync(ct);
        var total = list.Sum();
        var count = await q.CountAsync(ct);

        return new ThisMonthSalesDto(year, month, total, count);
    }
}

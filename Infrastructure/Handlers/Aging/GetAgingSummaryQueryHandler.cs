using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

public class GetAgingSummaryQueryHandler : IRequestHandler<GetAgingSummaryQuery, List<PartnerAgingSummaryDto>>
{
    private readonly AppDbContext _db;
    private readonly AgingService _aging;
    public GetAgingSummaryQueryHandler(AppDbContext db, AgingService aging)
    {
        _db = db;
        _aging = aging;
    }
    public async Task<List<PartnerAgingSummaryDto>> Handle(GetAgingSummaryQuery request, CancellationToken ct)
    {
        var partners = await _db.Partners
            .Where(p => request.Role == null || p.Role.ToString() == request.Role)
            .ToListAsync(ct);
        var result = new List<PartnerAgingSummaryDto>();
        foreach (var p in partners)
        {
            var aging = await _aging.GetPartnerAgingAsync(p.Id, request.AsOf, ct);
            result.Add(new PartnerAgingSummaryDto
            {
                PartnerId = p.Id,
                Title = p.Title,
                Total = aging.Total,
                Buckets = aging.Buckets,
                CreditLimitTry = p.CreditLimitTry
            });
        }
        return result.OrderByDescending(x => x.Total).ToList();
    }
}

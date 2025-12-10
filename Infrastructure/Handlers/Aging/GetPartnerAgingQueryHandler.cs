using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

public class GetPartnerAgingQueryHandler : IRequestHandler<GetPartnerAgingQuery, PartnerAgingDto>
{
    private readonly AgingService _aging;
    public GetPartnerAgingQueryHandler(AgingService aging) => _aging = aging;
    public async Task<PartnerAgingDto> Handle(GetPartnerAgingQuery request, CancellationToken ct)
        => await _aging.GetPartnerAgingAsync(request.PartnerId, request.AsOf, ct);
}

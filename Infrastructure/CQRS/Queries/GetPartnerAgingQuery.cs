using MediatR;

public sealed record GetPartnerAgingQuery(int PartnerId, DateTime? AsOf = null) : IRequest<PartnerAgingDto>;

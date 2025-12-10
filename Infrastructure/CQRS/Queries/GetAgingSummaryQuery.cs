using MediatR;

public sealed record GetAgingSummaryQuery(DateTime? AsOf = null, string? Role = "CUSTOMER") : IRequest<List<PartnerAgingSummaryDto>>;

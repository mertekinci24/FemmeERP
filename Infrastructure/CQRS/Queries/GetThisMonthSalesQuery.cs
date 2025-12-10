using MediatR;

namespace InventoryERP.Infrastructure.CQRS.Queries;

public record GetThisMonthSalesQuery(int? Year = null, int? Month = null) : IRequest<ThisMonthSalesDto>;

public record ThisMonthSalesDto(int Year, int Month, decimal TotalTry, int InvoiceCount);

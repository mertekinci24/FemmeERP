using MediatR;

namespace InventoryERP.Infrastructure.CQRS.Commands;

public record DeleteProductCommand(int Id) : IRequest;

using MediatR;
using System.Threading;
using System.Threading.Tasks;
using InventoryERP.Infrastructure.Commands.Invoices;
using InventoryERP.Infrastructure.Services;
using FluentValidation;

namespace InventoryERP.Infrastructure.Handlers.Invoices
{
    public class AddInvoiceLineHandler : IRequestHandler<AddInvoiceLineCommand, Unit>
    {
    private readonly IValidator<AddInvoiceLineCommand> _validator;
    private readonly IInvoicePostingService _postingService;

        public AddInvoiceLineHandler(IValidator<AddInvoiceLineCommand> validator, IInvoicePostingService postingService)
        {
            _validator = validator;
            _postingService = postingService;
        }

        public async Task<Unit> Handle(AddInvoiceLineCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            await _postingService.AddLineAsync(request, cancellationToken);
            return Unit.Value;
        }
    }
}

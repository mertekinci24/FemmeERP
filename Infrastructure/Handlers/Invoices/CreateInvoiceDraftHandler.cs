using MediatR;
using System.Threading;
using System.Threading.Tasks;
using InventoryERP.Infrastructure.Commands.Invoices;
using InventoryERP.Infrastructure.Services;
using FluentValidation;

namespace InventoryERP.Infrastructure.Handlers.Invoices
{
    public class CreateInvoiceDraftHandler : IRequestHandler<CreateInvoiceDraftCommand, Unit>
    {
    private readonly IValidator<CreateInvoiceDraftCommand> _validator;
    private readonly IInvoicePostingService _postingService;

        public CreateInvoiceDraftHandler(IValidator<CreateInvoiceDraftCommand> validator, IInvoicePostingService postingService)
        {
            _validator = validator;
            _postingService = postingService;
        }

        public async Task<Unit> Handle(CreateInvoiceDraftCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            await _postingService.CreateDraftAsync(request, cancellationToken);
            return Unit.Value;
        }
    }
}

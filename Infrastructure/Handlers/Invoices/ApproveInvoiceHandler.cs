using MediatR;
using System.Threading;
using System.Threading.Tasks;
using InventoryERP.Infrastructure.Commands.Invoices;
using InventoryERP.Infrastructure.Services;
using FluentValidation;

namespace InventoryERP.Infrastructure.Handlers.Invoices
{
    public class ApproveInvoiceHandler : IRequestHandler<ApproveInvoiceCommand, Unit>
    {
    private readonly IValidator<ApproveInvoiceCommand> _validator;
    private readonly IInvoicePostingService _postingService;

        public ApproveInvoiceHandler(IValidator<ApproveInvoiceCommand> validator, IInvoicePostingService postingService)
        {
            _validator = validator;
            _postingService = postingService;
        }

        public async Task<Unit> Handle(ApproveInvoiceCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            await _postingService.ApproveAsync(request, cancellationToken);
            return Unit.Value;
        }
    }
}

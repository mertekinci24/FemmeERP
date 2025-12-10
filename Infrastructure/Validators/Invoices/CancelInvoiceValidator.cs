using FluentValidation;
using InventoryERP.Infrastructure.Commands.Invoices;

namespace InventoryERP.Infrastructure.Validators.Invoices;

public class CancelInvoiceValidator : AbstractValidator<CancelInvoiceCommand>
{
    public CancelInvoiceValidator()
    {
        RuleFor(x => x.DocId).GreaterThan(0);
        // Belge APPROVED olmalı ve reversal yok kontrolü handler'da yapılacak.
    }
}

using FluentValidation;
using InventoryERP.Infrastructure.Commands.Invoices;

namespace InventoryERP.Infrastructure.Validators.Invoices;

public class ApproveInvoiceValidator : AbstractValidator<ApproveInvoiceCommand>
{
    public ApproveInvoiceValidator()
    {
        RuleFor(x => x.DocId).GreaterThan(0);
        // Belge DRAFT olmalı, satır ≥1, FxRate>0 (TRY değilse) kontrolü handler'da yapılacak.
    }
}

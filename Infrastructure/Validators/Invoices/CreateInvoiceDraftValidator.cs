using FluentValidation;
using InventoryERP.Infrastructure.Commands.Invoices;

namespace InventoryERP.Infrastructure.Validators.Invoices;

public class CreateInvoiceDraftValidator : AbstractValidator<CreateInvoiceDraftCommand>
{
    public CreateInvoiceDraftValidator()
    {
        RuleFor(x => x.PartnerId).GreaterThan(0);
        RuleFor(x => x.Currency).Length(3);
        RuleFor(x => x.FxRate).Must((cmd, fx) => cmd.Currency == "TRY" ? true : fx.HasValue && fx.Value > 0).WithMessage("TRY dışı FxRate > 0 olmalı.");
    }
}

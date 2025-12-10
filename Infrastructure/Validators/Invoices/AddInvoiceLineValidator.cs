using FluentValidation;
using InventoryERP.Infrastructure.Commands.Invoices;

namespace InventoryERP.Infrastructure.Validators.Invoices;

public class AddInvoiceLineValidator : AbstractValidator<AddInvoiceLineCommand>
{
    public AddInvoiceLineValidator()
    {
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VatRate).Must(v => v == 1 || v == 10 || v == 20).WithMessage("KDV oranı 1, 10 veya 20 olmalı.");
    }
}

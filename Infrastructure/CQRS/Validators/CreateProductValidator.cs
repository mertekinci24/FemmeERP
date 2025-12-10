using FluentValidation;
using InventoryERP.Infrastructure.CQRS.Commands;

namespace InventoryERP.Infrastructure.CQRS.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.BaseUom).NotEmpty().MaximumLength(8);
        RuleFor(x => x.VatRate).Must(x => x == 1 || x == 10 || x == 20);
    }
}

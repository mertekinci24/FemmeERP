using InventoryERP.Application.Partners;
using FluentValidation;

namespace InventoryERP.Infrastructure.Validators
{
    public class PartnerValidator : AbstractValidator<PartnerDetailDto>
    {
        public PartnerValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Cari adı zorunlu.");
            RuleFor(x => x.Role).NotEmpty().WithMessage("Rol zorunlu.");
            RuleFor(x => x.TaxNo).NotEmpty().WithMessage("Vergi no zorunlu.");
            RuleFor(x => x.BalanceTry).GreaterThanOrEqualTo(0).WithMessage("Bakiye negatif olamaz.");
            RuleFor(x => x.CreditLimitTry).GreaterThanOrEqualTo(0).WithMessage("Kredi limiti negatif olamaz.");
        }
    }
}

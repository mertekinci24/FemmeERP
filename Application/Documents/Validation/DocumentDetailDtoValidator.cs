using FluentValidation;
using InventoryERP.Application.Documents.DTOs;
using System.Linq;

namespace InventoryERP.Application.Documents.Validation
{
    public class DocumentDetailDtoValidator : AbstractValidator<DocumentDetailDto>
    {
        public DocumentDetailDtoValidator()
        {
            RuleFor(x => x.PartnerId).GreaterThan(0);
            RuleFor(x => x.Lines).NotNull().Must(l => l.Count > 0);
            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.Qty).GreaterThan(0);
                line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
                line.RuleFor(l => l.VatRate).Must(v => new[] { 1, 10, 20 }.Contains(v));
            });
            RuleFor(x => x.Currency).NotEmpty();
            // Add more rules as needed (e.g., TRY currency rules)
        }
    }
}

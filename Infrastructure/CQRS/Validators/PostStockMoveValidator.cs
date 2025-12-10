using FluentValidation;
using InventoryERP.Infrastructure.CQRS.Commands;

namespace InventoryERP.Infrastructure.CQRS.Validators;

public class PostStockMoveValidator : AbstractValidator<PostStockMoveCommand>
{
    public PostStockMoveValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.Direction).Must(x => x == "in" || x == "out");
    }
}

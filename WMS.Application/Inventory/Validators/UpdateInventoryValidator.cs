using FluentValidation;
using WMS.Application.Inventory.DTOs;

namespace WMS.Application.Inventory.Validators;

public class UpdateInventoryValidator : AbstractValidator<UpdateInventoryRequest>
{
    public UpdateInventoryValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(255).When(x => x.Name is not null);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).When(x => x.Quantity.HasValue);
    }
}
using FluentValidation;
using WMS.Application.Inventory.DTOs;

namespace WMS.Application.Inventory.Validators;

public class UpdateInventoryValidator : AbstractValidator<UpdateInventoryRequest>
{
    public UpdateInventoryValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).When(x => x.Quantity.HasValue).WithMessage("Số lượng phải >= 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).When(x => x.UnitPrice.HasValue).WithMessage("Đơn giá phải >= 0");
    }
}
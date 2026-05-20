using FluentValidation;
using WMS.Application.Inventory.DTOs;

namespace WMS.Application.Inventory.Validators;

public class CreateInventoryValidator : AbstractValidator<CreateInventoryRequest>
{
    public CreateInventoryValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU không được để trống")
            .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống")
            .MaximumLength(255);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Số lượng phải >= 0");

        RuleFor(x => x.MinQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Đơn giá phải lớn hơn 0");
    }
}

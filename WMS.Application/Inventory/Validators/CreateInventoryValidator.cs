using FluentValidation;
using WMS.Application.Inventory.DTOs;

namespace WMS.Application.Inventory.Validators;

public class CreateInventoryValidator : AbstractValidator<CreateInventoryRequest>
{
    public CreateInventoryValidator()
    {
        RuleFor(x => x.SkuId)
            .NotEmpty().WithMessage("SkuId không được để trống");

        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("LocationId không được để trống");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Số lượng phải >= 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Đơn giá phải >= 0");

        RuleFor(x => x.PutawayDate)
            .NotEmpty().WithMessage("Ngày lưu kho không được để trống");
    }
}

using FluentValidation;
using WMS.Application.Inbound.DTOs;

namespace WMS.Application.Inbound.Validators;

public class CreateInboundValidator
    : AbstractValidator<CreateInboundRequest>
{
    public CreateInboundValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Nhà cung cấp không được để trống");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Đơn phải có ít nhất 1 sản phẩm");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.InventoryItemId)
                .NotEmpty();
            item.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Số lượng phải lớn hơn 0");
        });
    }
}
using FluentValidation;
using WMS.Application.Outbound.DTOs;

namespace WMS.Application.Outbound.Validators;

public class CreateOutboundValidator : AbstractValidator<CreateOutboundRequest>
{
    public CreateOutboundValidator()
    {
        RuleFor(x => x.PartnerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Đơn phải có ít nhất 1 sản phẩm");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.InventoryItemId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

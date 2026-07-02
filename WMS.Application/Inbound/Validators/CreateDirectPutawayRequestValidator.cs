using FluentValidation;
using WMS.Application.Inbound.DTOs;

namespace WMS.Application.Inbound.Validators;

public class CreateDirectPutawayRequestValidator : AbstractValidator<CreateDirectPutawayRequest>
{
    public CreateDirectPutawayRequestValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("WarehouseId is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Items list cannot be empty.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.SkuId)
                .NotEmpty().WithMessage("SkuId is required.");

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

            item.RuleFor(x => x)
                .Must(x => string.IsNullOrEmpty(x.SerialNumber) || x.Quantity == 1)
                .WithMessage("Quantity must be 1 when Serial Number is specified.")
                .WithName("Quantity");
        });
    }
}

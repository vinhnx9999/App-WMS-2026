using FluentValidation;
using WMS.Application.Product.Skus.Commands.CreateSku;

namespace WMS.Application.Product.Skus.Validators;

public sealed class CreateSkuCommandValidator : AbstractValidator<CreateSkuCommand>
{
    public CreateSkuCommandValidator()
    {
        RuleFor(x => x.SkuCode)
            .Must(skuCode => !string.IsNullOrWhiteSpace(skuCode))
            .WithMessage("SKU code is required");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Price.HasValue)
            .WithMessage("SKU price cannot be negative");
    }
}

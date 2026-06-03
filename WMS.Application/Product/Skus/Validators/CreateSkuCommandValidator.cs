using FluentValidation;
using WMS.Application.Product.Skus.Commands.CreateSku;

namespace WMS.Application.Product.Skus.Validators;

public sealed class CreateSkuCommandValidator : AbstractValidator<CreateSkuCommand>
{
    public CreateSkuCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.SkuCode)
            .Must(skuCode => string.IsNullOrWhiteSpace(skuCode) || skuCode.Trim().Length > 0)
            .When(x => x.SkuCode is not null)
            .WithMessage("SKU code cannot be empty.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Price.HasValue)
            .WithMessage("SKU price cannot be negative.");
    }
}

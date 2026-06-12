using FluentValidation;
using WMS.Application.Skus.Commands.UpdateSku;

namespace WMS.Application.Skus.Validators;

public sealed class UpdateSkuCommandValidator : AbstractValidator<UpdateSkuCommand>
{
    public UpdateSkuCommandValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => name is null || !string.IsNullOrWhiteSpace(name))
            .WithMessage("SKU name cannot be empty");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Price.HasValue)
            .WithMessage("SKU price cannot be negative");
    }
}

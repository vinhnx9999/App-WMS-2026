using FluentValidation;
using WMS.Application.Product.Skus.Commands.ImportSkus;

namespace WMS.Application.Product.Skus.Validators;

public sealed class ImportSkusCommandValidator : AbstractValidator<ImportSkusCommand>
{
    public ImportSkusCommandValidator()
    {
        RuleFor(x => x.Rows)
            .NotNull()
            .WithMessage("Rows are required")
            .NotEmpty()
            .WithMessage("Rows cannot be empty");

        RuleFor(x => x.Mode)
            .IsInEnum()
            .WithMessage("Import mode is invalid");

        RuleForEach(x => x.Rows)
            .ChildRules(row =>
            {
                row.RuleFor(x => x.RowNumber)
                    .GreaterThan(0)
                    .WithMessage("Row number must be greater than 0");
            })
            .When(x => x.Rows is not null);
    }
}

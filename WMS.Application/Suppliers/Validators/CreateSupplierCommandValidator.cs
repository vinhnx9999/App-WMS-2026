using FluentValidation;
using WMS.Application.Suppliers.Commands.CreateSupplier;

namespace WMS.Application.Suppliers.Validators;

public sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Supplier code must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Supplier name is required.")
            .MaximumLength(255).WithMessage("Supplier name must not exceed 255 characters.");

        RuleFor(x => x.Contact)
            .MaximumLength(255).WithMessage("Contact name must not exceed 255 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.Email)
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.")
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email)).WithMessage("Invalid email format.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");
    }
}

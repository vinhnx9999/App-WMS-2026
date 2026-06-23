using FluentValidation;
using WMS.Application.Customers.Commands.UpdateCustomer;

namespace WMS.Application.Customers.Validators;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(255).WithMessage("Customer name must not exceed 255 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(255).WithMessage("Address must not exceed 255 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.Type)
            .MaximumLength(50).WithMessage("Type must not exceed 50 characters.");
    }
}

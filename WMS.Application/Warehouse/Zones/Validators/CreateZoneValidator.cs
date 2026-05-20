using FluentValidation;
using WMS.Application.Warehouse.Zones.DTOs;

namespace WMS.Application.Warehouse.Zones.Validators;

public class CreateZoneValidator : AbstractValidator<CreateZoneRequest>
{
    public CreateZoneValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ZoneCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.TotalLocations).GreaterThan(0);
    }
}
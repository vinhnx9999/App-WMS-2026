using FluentValidation;
using WMS.Application.Common.Models;

namespace DP.AppWMS.ApiService.Filters;

public static class ValidationFilter
{
    public static async Task ValidateAsync<T>(IValidator<T> validator, T model)
    {
        var result = await validator.ValidateAsync(model);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => new
            {
                field = e.PropertyName,
                message = e.ErrorMessage
            });
            throw new AppException(400, "VALIDATION_ERROR",
                System.Text.Json.JsonSerializer.Serialize(errors));
        }
    }
}
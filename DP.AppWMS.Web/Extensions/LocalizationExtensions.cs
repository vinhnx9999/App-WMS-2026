using Microsoft.AspNetCore.Localization;

namespace DP.AppWMS.Web.Extensions
{
    public static class LocalizationExtensions
    {
        public static IEndpointRouteBuilder MapCultureEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/Culture/Set", (string? culture, string? redirectUri, HttpContext httpContext) =>
            {
                if (!string.IsNullOrWhiteSpace(culture))
                {
                    httpContext.Response.Cookies.Append(
                        CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture))
                    );
                }
                if (string.IsNullOrEmpty(redirectUri) || !PathString.FromUriComponent(redirectUri).StartsWithSegments("/"))
                {
                    redirectUri = "/";
                }
                return Results.LocalRedirect(redirectUri);
            });
            return endpoints;
        }

    }
}

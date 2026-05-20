using System.IdentityModel.Tokens.Jwt;
using WMS.Application.Auth.Interfaces;
namespace DP.AppWMS.ApiService.Middlewares;

public class TokenRevocationMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(
        HttpContext ctx, ITokenRevocationStore revocationStore)
    {
        // Chỉ check nếu user đã authenticated (JWT valid)
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var jti = ctx.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jti))
            {
                var isRevoked = await revocationStore.IsRevokedAsync(jti);

                if (isRevoked)
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        error = new
                        {
                            code = "TOKEN_REVOKED",
                            message = "Token đã bị thu hồi. Vui lòng đăng nhập lại."
                        }
                    });
                    return;
                }
            }
        }

        await _next(ctx);
    }
}
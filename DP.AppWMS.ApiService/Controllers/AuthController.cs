using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Auth.DTOs.Facebook;
using WMS.Application.Auth.DTOs.Google;
using WMS.Application.Auth.DTOs.Login;
using WMS.Application.Auth.DTOs.Logout;
using WMS.Application.Auth.DTOs.Token;
using WMS.Application.Auth.DTOs.Twitter;
using WMS.Application.Auth.Interfaces;
using WMS.Application.Common.Models;

namespace DP.AppWMS.ApiService.Controllers;

[ApiController]
[Route("v1/auth")]
public class AuthController(
    IAuthService auth,
    IGoogleAuthService googleAuth,
    IFacebookAuthService facebookAuth,
    IXAuthService xAuth,
    IMicrosoftAuthService msAuth,
    ILinkedInAuthService linkedInAuth,
    IConfiguration config
) : ControllerBase
{
    // ═══ Existing: Email/Password login ═══
    [HttpPost("login")]
    public async Task<ActionResult> Login(
        [FromBody] LoginRequest r, CancellationToken ct) =>
        Ok(ApiResponse<LoginResponse>.Ok(await auth.LoginAsync(r, ct)));

    /// <summary>
    /// Login bằng Google ID Token.
    /// Frontend dùng Google Sign-In SDK lấy id_token,
    /// POST lên đây.
    ///
    /// POST /v1/auth/google
    /// Body: { "idToken": "eyJhbGciOiJSUzI1..." }
    /// </summary>
    [HttpPost("google")]
    public async Task<ActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(new { error = "Thiếu idToken" });

        var result = await googleAuth.LoginWithIdTokenAsync(request.IdToken, ct);
        return Ok(ApiResponse<GoogleLoginResponse>.Ok(result,
            result.IsNewUser ? "Tài khoản mới đã được tạo" : "Đăng nhập thành công"));
    }

    /// <summary>
    /// Redirect user to Google consent screen.
    ///
    /// GET /v1/auth/google/redirect
    /// </summary>
    [HttpGet("google/redirect")]
    public IActionResult GoogleRedirect([FromQuery] string? returnUrl)
    {
        var baseUrl = config["Google:RedirectUri"]
            ?? $"{Request.Scheme}://{Request.Host}/v1/auth/google/callback";

        var state = string.IsNullOrEmpty(returnUrl)
            ? "/"
            : Uri.EscapeDataString(returnUrl);

        var url = googleAuth.GetGoogleAuthUrl(baseUrl, state);

        // 302 redirect đến Google
        return Redirect(url);
    }

    /// <summary>
    /// Google callback — exchange code for JWT.
    /// Google redirect user về đây sau khi approve.
    ///
    /// GET /v1/auth/google/callback?code=4/0Axx...&state=...
    /// </summary>
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string code,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { error = "Missing code" });

        try
        {
            var redirectUri = config["Google:RedirectUri"]
                ?? $"{Request.Scheme}://{Request.Host}/v1/auth/google/callback";

            var result = await googleAuth.LoginWithAuthCodeAsync(
                code, redirectUri, ct);

            // Decode state → frontend URL
            var frontendUrl = config["Google:FrontendCallbackUrl"]
                ?? "http://localhost:5173/auth/callback";

            if (!string.IsNullOrEmpty(state) && state != "/")
            {
                // state chứa returnUrl từ frontend
                var decodedState = Uri.UnescapeDataString(state);
                if (Uri.IsWellFormedUriString(decodedState, UriKind.Absolute))
                    frontendUrl = decodedState;
            }

            // Redirect về frontend kèm token
            var redirectUrl = QueryString.Create(new Dictionary<string, string?>
            {
                ["token"] = result.AccessToken,
                ["expires_in"] = result.ExpiresIn.ToString(),
                ["new_user"] = result.IsNewUser.ToString().ToLower(),
            });

            return Redirect($"{frontendUrl}{redirectUrl}");
        }
        catch (AppException ex)
        {
            // Redirect về frontend kèm error
            var frontendUrl = config["Google:FrontendCallbackUrl"]
                ?? "http://localhost:5173/auth/callback";
            return Redirect($"{frontendUrl}?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    /// <summary>
    /// Login bằng Facebook access_token (Flow A — Client-side).
    ///
    /// Frontend dùng FB.login() lấy access_token,
    /// POST lên đây.
    ///
    /// POST /v1/auth/facebook
    /// Body: { "accessToken": "EAAxxxx..." }
    /// </summary>
    [HttpPost("facebook")]
    public async Task<ActionResult> FacebookLogin(
        [FromBody] FacebookLoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            return BadRequest(new { error = "Thiếu accessToken" });

        var result = await facebookAuth.LoginAsync(request.AccessToken, ct);

        return Ok(ApiResponse<FacebookLoginResponse>.Ok(result,
            result.IsNewUser ? "Tài khoản mới đã được tạo" : "Đăng nhập thành công"));
    }

    /// <summary>
    /// Redirect to Facebook consent screen (Flow B).
    ///
    /// GET /v1/auth/facebook/redirect
    /// </summary>
    [HttpGet("facebook/redirect")]
    public IActionResult FacebookRedirect([FromQuery] string? returnUrl)
    {
        var redirectUri = config["Facebook:RedirectUri"]
            ?? $"{Request.Scheme}://{Request.Host}/v1/auth/facebook/callback";

        var state = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;

        return Redirect(facebookAuth.GetFacebookAuthUrl(redirectUri, state));
    }

    /// <summary>
    /// Facebook callback — exchange code for JWT (Flow B).
    ///
    /// GET /v1/auth/facebook/callback?code=AQBx...&state=...
    /// </summary>
    [HttpGet("facebook/callback")]
    public async Task<IActionResult> FacebookCallback(
        [FromQuery] string code,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { error = "Missing code" });

        try
        {
            var redirectUri = config["Facebook:RedirectUri"]
                ?? $"{Request.Scheme}://{Request.Host}/v1/auth/facebook/callback";

            var result = await facebookAuth.LoginWithCodeAsync(
                code, redirectUri, ct);

            var frontendUrl = config["Facebook:FrontendCallbackUrl"]
                ?? config["Google:FrontendCallbackUrl"]
                ?? "http://localhost:5173/auth/callback";

            return Redirect($"{frontendUrl}?token={result.AccessToken}&new_user={result.IsNewUser}");
        }
        catch (AppException ex)
        {
            var frontendUrl = config["Facebook:FrontendCallbackUrl"]
                ?? config["Google:FrontendCallbackUrl"]
                ?? "http://localhost:5173/auth/callback";
            return Redirect($"{frontendUrl}?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    /// <summary>
    /// Redirect to X.
    ///
    /// GET /v1/auth/x/redirect
    /// </summary>
    [HttpGet("x/redirect")]
    public async Task<ActionResult> XRedirect(CancellationToken ct)
    {
        var redirectUri = config["X:RedirectUri"]
            ?? $"{Request.Scheme}://{Request.Host}/v1/auth/x/callback";

        var result = await xAuth.GetAuthUrlAsync(redirectUri, ct);

        return Ok(ApiResponse<XAuthRedirectResponse>.Ok(result));
    }

    /// <summary>
    /// Bước 2: X callback — exchange code for JWT.
    ///
    /// GET /v1/auth/x/callback?code=xxx&state=yyy
    /// </summary>
    [HttpGet("x/callback")]
    public async Task<IActionResult> XCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(new { error = "Missing code or state" });

        try
        {
            var redirectUri = config["X:RedirectUri"]
                ?? $"{Request.Scheme}://{Request.Host}/v1/auth/x/callback";

            var result = await xAuth.LoginWithCodeAsync(
                code, state, redirectUri, ct);

            // Redirect về frontend
            var feUrl = config["X:FrontendCallbackUrl"]
                ?? config["Google:FrontendCallbackUrl"]
                ?? "http://localhost:5173/auth/callback";

            return Redirect(
                $"{feUrl}?token={result.AccessToken}"
              + $"&refresh_token={result.RefreshToken}"
              + $"&new_user={result.IsNewUser}");
        }
        catch (AppException ex)
        {
            var feUrl = config["X:FrontendCallbackUrl"]
                ?? config["Google:FrontendCallbackUrl"]
                ?? "http://localhost:5173/auth/callback";
            return Redirect($"{feUrl}?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    /// <summary>
    /// Redirect to Microsoft login.
    ///
    /// GET /v1/auth/microsoft/redirect?returnUrl=/
    /// </summary>
    [HttpGet("microsoft/redirect")]
    public IActionResult MicrosoftRedirect([FromQuery] string? returnUrl)
    {
        var redirectUri = config["Microsoft:RedirectUri"]
            ?? $"{Request.Scheme}://{Request.Host}/v1/auth/microsoft/callback";

        var authUrl = msAuth.GetAuthUrl(redirectUri, returnUrl);
        return Redirect(authUrl);
    }

    /// <summary>
    /// Microsoft callback — exchange code for JWT.
    ///
    /// GET /v1/auth/microsoft/callback?code=xxx&state=yyy
    /// </summary>
    [HttpGet("microsoft/callback")]
    public async Task<IActionResult> MicrosoftCallback(
        [FromQuery] string code,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { error = "Missing code" });

        try
        {
            var redirectUri = config["Microsoft:RedirectUri"]
                ?? $"{Request.Scheme}://{Request.Host}/v1/auth/microsoft/callback";

            var result = await msAuth.LoginWithCodeAsync(code, redirectUri, ct);

            // Decode state → frontend URL
            var feUrl = config["Microsoft:FrontendCallbackUrl"]
                ?? config["Google:FrontendCallbackUrl"]
                ?? "http://localhost:5173/auth/callback";

            return Redirect(
                $"{feUrl}?token={result.AccessToken}"
              + $"&new_user={result.IsNewUser}");
        }
        catch (AppException ex)
        {
            var feUrl = config["Microsoft:FrontendCallbackUrl"]
                ?? config["Google:FrontendCallbackUrl"]
                ?? "http://localhost:5173/auth/callback";
            return Redirect($"{feUrl}?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    /// <summary>
    /// Redirect to LinkedIn login.
    ///
    /// GET /v1/auth/linkedin/redirect
    /// </summary>
    [HttpGet("linkedin/redirect")]
    public IActionResult LinkedInRedirect([FromQuery] string? returnUrl)
    {
        var redirectUri = config["LinkedIn:RedirectUri"]
            ?? $"{Request.Scheme}://{Request.Host}/v1/auth/linkedin/callback";

        var authUrl = linkedInAuth.GetAuthUrl(redirectUri, returnUrl);
        return Redirect(authUrl);
    }

    /// <summary>
    /// LinkedIn callback — exchange code for JWT.
    ///
    /// GET /v1/auth/linkedin/callback?code=xxx&state=yyy
    /// </summary>
    [HttpGet("linkedin/callback")]
    public async Task<IActionResult> LinkedInCallback(
        [FromQuery] string code,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { error = "Missing code" });

        try
        {
            var redirectUri = config["LinkedIn:RedirectUri"]
                ?? $"{Request.Scheme}://{Request.Host}/v1/auth/linkedin/callback";

            var result = await linkedInAuth.LoginWithCodeAsync(
                code, redirectUri, ct);

            var feUrl = config["LinkedIn:FrontendCallbackUrl"]
                ?? config["Google:FrontendCallbackUrl"]
                ?? "http://localhost:5173/auth/callback";

            return Redirect(
                $"{feUrl}?token={result.AccessToken}"
              + $"&new_user={result.IsNewUser}");
        }
        catch (AppException ex)
        {
            var feUrl = config["LinkedIn:FrontendCallbackUrl"]
                ?? config["Google:FrontendCallbackUrl"]
                ?? "http://localhost:5173/auth/callback";
            return Redirect($"{feUrl}?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    // ═══ Get current user (shared) ═══
    [HttpGet("me")]
    public async Task<ActionResult> Me(CancellationToken ct) =>
        Ok(ApiResponse.Ok(await auth.GetProfileAsync(ct)));

    /// <summary>
    /// Logout — revoke token(s).
    /// 
    /// POST /v1/auth/logout
    /// Authorization: Bearer {jwt}
    /// Body: {
    ///   "refreshToken": "...",        ← optional (nếu có)
    ///   "logoutAllDevices": false      ← true = logout all devices
    /// }
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout(
        [FromBody] LogoutRequest? request, CancellationToken ct)
    {
        await auth.LogoutAsync(request ?? new LogoutRequest(), ct);

        return Ok(ApiResponse<object>.Ok(null, "Đăng xuất thành công"));
    }

    /// <summary>
    /// Refresh — đổi refresh_token lấy access_token mới.
    /// Implements token rotation (old token revoked).    
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult> Refresh(
        [FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await auth.RefreshTokenAsync(request, ct);
        return Ok(ApiResponse<TokenResponse>.Ok(result, "Token đã được làm mới"));
    }
}

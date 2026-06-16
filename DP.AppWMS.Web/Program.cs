using DP.AppWMS.Web.Components;
using DP.AppWMS.Web.Extensions;
using DP.AppWMS.Web.Services.State;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
//builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Resource files for localization should be placed in the "Resources" folder.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add application services.
builder.Services.AddScoped<LayoutService>();

// Add MudBlazor services.
builder.Services.AddMudServices();

var app = builder.Build();
var supportedCultures = new[] { "vi", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// Support localization.
app.UseRequestLocalization(localizationOptions);


app.UseHttpsRedirection();

app.UseAntiforgery();

//app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapCultureEndpoints();

app.MapDefaultEndpoints();

app.Run();

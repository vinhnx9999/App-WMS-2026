using DP.AppWMS.ApiService.Middlewares;
using FluentValidation;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using WMS.Application;
using WMS.Application.OdooIntegration.HealthCheck;
using WMS.Application.SAPIntegration.HealthCheck;
using WMS.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register health checks
builder.Services.AddHealthChecks()
    .AddCheck<OdooConnectionHealthCheck>("odoo-connection")
    .AddCheck<SapConnectionHealthCheck>("sap-connection")
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/wms-.log", rollingInterval: RollingInterval.Day));


builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // General keep-alive timeout (applies to HTTP/1.1 and HTTP/2)
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);

    // Optional: limit concurrent connections
    serverOptions.Limits.MaxConcurrentConnections = 100;

    // Optional: request body size
    serverOptions.Limits.MaxRequestBodySize = 100_000_000;
});

// ─── SignalR ───
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(10);
    options.MaximumReceiveMessageSize = 64 * 1024; // 64 KB
})
.AddJsonProtocol(options =>
{
    // CamelCase cho JS client
    options.PayloadSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
})
.AddStackExchangeRedis(
    builder.Configuration.GetConnectionString("Redis") ?? "",
    options =>
    {
        options.Configuration.ChannelPrefix =
            StackExchange.Redis.RedisChannel.Literal("wms_signalr");
    });

// Controllers + FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IDependency).Assembly));

// Layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{    
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WMS API",
        Version = "v1.0"
    });

    // Define Bearer token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });

    // Apply requirement
    //c.AddSecurityRequirement(new OpenApiSecurityRequirement
    //{
    //    {
    //        new OpenApiSecurityScheme
    //        {
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            }
    //        },
    //        Array.Empty<string>()
    //    }
    //});
});

// CORS
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy("wms", p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// Pipeline behaviors
//builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
// thêm ValidationBehavior, PerformanceBehavior, DistributedCachingBehavior

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapOpenApi();
app.MapScalarApiReference();

if (app.Environment.IsDevelopment())
{   
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("wms");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TokenRevocationMiddleware>();

// Migrate + Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
    //await db.Database.MigrateAsync();
    //await SeedData.InitializeAsync(db);
}

app.Run();

internal class OpenApiReference
{
    public ReferenceType Type { get; set; }
    public string Id { get; set; } = "";
}
using DP.AppWMS.ApiService.Endpoints;
using DP.AppWMS.ApiService.Middlewares;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using System.Text;
using WMS.Application;
using WMS.Application.Common.Behaviors;
using WMS.Application.OdooIntegration.HealthCheck;
using WMS.Application.SAPIntegration.HealthCheck;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register health checks
var healthChecks = builder.Services.AddHealthChecks()
      .AddNpgSql(builder.Configuration.GetConnectionString("Default")!)
      .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

var erpProvider = builder.Configuration["ErpProvider"];
if (erpProvider == "Sap")
{
    healthChecks.AddCheck<SapConnectionHealthCheck>("sap-connection");
}
else if (erpProvider == "Odoo")
{
    healthChecks.AddCheck<OdooConnectionHealthCheck>("odoo-connection");
}


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
builder.Services.AddValidatorsFromAssembly(typeof(IDependency).Assembly);

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IDependency).Assembly));

// Layers
builder.Services.AddInfrastructure(builder.Configuration);

// Add authentication and authorization
builder.Services
      .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
          options.TokenValidationParameters = new TokenValidationParameters
          {
              ValidateIssuer = true,
              ValidateAudience = true,
              ValidateLifetime = true,
              ValidateIssuerSigningKey = true,
              ValidIssuer = builder.Configuration["Jwt:Issuer"],
              ValidAudience = builder.Configuration["Jwt:Audience"],
              IssuerSigningKey = new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
          };
      });
builder.Services.AddAuthorization();

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
{
    // only in development mode, allow all origins for testing purposes
    if (builder.Environment.IsDevelopment())
    {
        p.SetIsOriginAllowed(origin => true)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    }
    else
    {
        p.WithOrigins(origins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    }
}));

// Pipeline behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
//builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
// thêm PerformanceBehavior, DistributedCachingBehavior

// Register DI minimal API endpoints
builder.Services.AddEndpoints();

var app = builder.Build();

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
app.MapEndpoints();
app.MapControllers();
app.MapHealthChecks("/health");

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TokenRevocationMiddleware>();

// Migrate + Seed
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
//    await db.Database.MigrateAsync();
//    await SeedData.InitializeAsync(db);
//}

app.Run();

internal class OpenApiReference
{
    public ReferenceType Type { get; set; }
    public string Id { get; set; } = "";
}
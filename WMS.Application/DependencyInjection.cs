using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WMS.Application.Auth.Interfaces;
using WMS.Application.Auth.Services;
using WMS.Application.Auth.Services.AuthProvider;
using WMS.Application.Auth.Services.TokenRevocation;
using WMS.Application.BackgroundJobs;
using WMS.Application.Common.Service;
using WMS.Application.Inbound.Services;
using WMS.Application.Inventory.Services;
using WMS.Application.OdooIntegration.OdooInboundSync;
using WMS.Application.OdooIntegration.OdooMasterSync;
using WMS.Application.OdooIntegration.OdooOutboundSync;
using WMS.Application.OdooIntegration.OdooWebhook;
using WMS.Application.Outbound.Services;
using WMS.Application.Reports.Services;
using WMS.Application.SAPIntegration.SapInboundSync;
using WMS.Application.SAPIntegration.SapMasterSync;
using WMS.Application.SignalR;
using WMS.Application.Warehouse.Services;
using WMS.Application.Warehouse.Zones.Services;
using WMS.Domain.Interfaces;
using WMS.Domain.Interfaces.Warehouses;
using WMS.Domain.Orchestrator;
using WMS.Infrastructure.ERPs.Odoo.DataClient;
using WMS.Infrastructure.ERPs.Odoo.DataConfig;
using WMS.Infrastructure.ERPs.SAP.DataClient;
using WMS.Infrastructure.ERPs.SAP.DataConfig;
using WMS.Infrastructure.ERPs.SAP.RfcClient;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Repositories;
using WMS.Infrastructure.Security;

namespace WMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // PostgreSQL
        services.AddDbContext<WmsDbContext>(opt =>
            opt.UseNpgsql(
                config.GetConnectionString("Default"),
                pg =>
                {
                    pg.MigrationsAssembly(typeof(WmsDbContext).Assembly.FullName);
                    pg.EnableRetryOnFailure(3);
                }));

        // Redis
        var redisConn = config.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConn))
        {
            services.AddSingleton(ConnectionMultiplexer.Connect(redisConn));
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = ConfigurationOptions.Parse(redisConn);
                options.AbortOnConnectFail = false;   // Don't crash if Redis is down
                options.ConnectRetry = 3;
                options.ConnectTimeout = 5000;
                return ConnectionMultiplexer.Connect(options);
            });

            // ═══ Auth services ═══
            services.AddScoped<ITokenRevocationStore, TokenRevocationStore>();
        }
        else
        {
            // No Redis → use in-memory fallback
            services.AddSingleton<ITokenRevocationStore, InMemoryTokenRevocationStore>();
        }

        // Repositories & UoW
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICodeSequenceRepository, CodeSequenceRepository>();
        //services.AddScoped<ITransaction, EfTransaction>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISequenceCodeGenerator, SequenceCodeGenerator>();

        services.AddScoped<IDashboardNotifier, DashboardNotifier>();

        services.AddServices();

        // Current User
        services.AddScoped<ICurrentUser, CurrentUser>();

        // ERP
        var erp = config["ErpProvider"];
        if (erp == "Sap")
        {
            services.AddSapIntegration(config);
        }
        else if (erp == "Odoo")
        {
            services.AddOdooIntegration(config);
        }

        services.AddSingleton<IStringLocalizer>((sp) =>
        {
            var sharedLocalizer = sp.GetRequiredService<IStringLocalizer<MultiLanguage>>();
            return sharedLocalizer;
        });

        services.AddSingleton<CacheManager>();
        services.AddSingleton<IMemoryCache>(factory =>
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            return cache;
        });

        // Background Jobs
        services.AddHostedService<OutboxProcessor>();
        services.AddScoped<IOdooWebhookService, OdooWebhookService>();

        if (erp == "Sap")
        {
            services.AddHostedService<SapSyncBackgroundService>();
        }
        else if (erp == "Odoo")
        {
            services.AddHostedService<OdooSyncBackgroundService>();
            services.AddHostedService<WebhookRetryJob>();
        }

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFacebookAuthService, FacebookAuthService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IXAuthService, XAuthService>();
        services.AddScoped<IMicrosoftAuthService, MicrosoftAuthService>();
        services.AddScoped<ILinkedInAuthService, LinkedInAuthService>();

        services.AddScoped<IInboundService, InboundService>();
        services.AddScoped<IOutboundService, OutboundService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IReportService, ReportService>();

        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<WarehouseProvisioningService>();
        services.AddScoped<IWarehouseRuleResolutionService, WarehouseRuleResolutionService>();
        services.AddScoped<InboundWorkflowOrchestrator>();
        services.AddScoped<WMS.Domain.Services.PalletPutawayDomainService>();

        return services;
    }

    public static IServiceCollection AddSapIntegration(this IServiceCollection services, IConfiguration config)
    {
        var sapConfig = config.GetSection("Sap").Get<SapConfig>();

        if (sapConfig?.Enabled != true)
        {
            // Register no-op implementations
            services.AddSingleton<ISapODataClient, NoOpSapODataClient>();
            services.AddSingleton<ISapRfcClient, NoOpSapRfcClient>();
            return services;
        }

        services.Configure<SapConfig>(config.GetSection("Sap"));

        // HTTP Client for OData
        services.AddHttpClient("SapOData", client =>
        {
            client.BaseAddress = new Uri(sapConfig.OData.BaseUrl);
        });

        services.AddScoped<ISapODataClient, SapODataClient>();
        services.AddScoped<ISapRfcClient, SapRfcClient>();
        services.AddScoped<ISapInboundSyncService, SapInboundSyncService>();
        services.AddScoped<ISapMasterSyncService, SapMasterSyncService>();

        return services;
    }

    public static IServiceCollection AddOdooIntegration(
    this IServiceCollection services, IConfiguration config)
    {
        var provider = config["ErpProvider"];

        if (provider != "Odoo")
        {
            // Register no-op
            services.AddSingleton<IOdooClient, NoOpOdooClient>();
            return services;
        }

        services.Configure<OdooConfig>(config.GetSection("Odoo"));

        services.AddHttpClient("Odoo", (sp, client) =>
        {
            var cfg = sp.GetRequiredService<IOptions<OdooConfig>>().Value;
            client.BaseAddress = new Uri(cfg.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddScoped<IOdooClient, OdooJsonRpcClient>();
        services.AddScoped<IOdooInboundSyncService, OdooInboundSyncService>();
        services.AddScoped<IOdooOutboundSyncService, OdooOutboundSyncService>();
        services.AddScoped<IOdooMasterSyncService, OdooMasterSyncService>();

        return services;
    }
}


using DP.AppWMS.ApiService.Endpoints.Skus;

namespace DP.AppWMS.ApiService.Endpoints
{
    public static class EndpointExtensions
    {
        public static IServiceCollection AddEndpoints(this IServiceCollection services)
        {
            services.AddSingleton<IEndpoint, SkuEndpoints>();

            return services;
        }

        public static WebApplication MapEndpoints(this WebApplication app)
        {
            var endpoints = app.Services.GetServices<IEndpoint>();
            foreach (var endpoint in endpoints)
            {
                endpoint.MapEndpoint(app);
            }
            return app;
        }
    }
}

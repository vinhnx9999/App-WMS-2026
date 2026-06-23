using DP.AppWMS.ApiService.Endpoints.Categories;
using DP.AppWMS.ApiService.Endpoints.Customers;
using DP.AppWMS.ApiService.Endpoints.Products;
using DP.AppWMS.ApiService.Endpoints.Skus;
using DP.AppWMS.ApiService.Endpoints.Suppliers;

namespace DP.AppWMS.ApiService.Endpoints
{
    public static class EndpointExtensions
    {
        public static IServiceCollection AddEndpoints(this IServiceCollection services)
        {
            services.AddSingleton<IEndpoint, SkuEndpoints>();
            services.AddSingleton<IEndpoint, ProductEndpoints>();
            services.AddSingleton<IEndpoint, CategoryEndpoints>();
            services.AddSingleton<IEndpoint, SupplierEndpoints>();
            services.AddSingleton<IEndpoint, CustomerEndpoints>();

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

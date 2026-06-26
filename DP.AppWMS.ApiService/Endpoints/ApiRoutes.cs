namespace DP.AppWMS.ApiService.Endpoints;

public static class ApiRoutes
{
    public const string Prefix = "api";
    public const string Version = "v1";
    public const string Base = $"/{Prefix}/{Version}";

    public static class Resources
    {
        public const string Skus = "skus";
        public const string Products = "products";
        public const string Categories = "categories";
        public const string Suppliers = "suppliers";
        public const string Customers = "customers";
        public const string Warehouses = "warehouses";
        public const string Inbound = "inbound";
    }

    public static class Groups
    {
        public const string Skus = $"{Base}/{Resources.Skus}";
        public const string Products = $"{Base}/{Resources.Products}";
        public const string Categories = $"{Base}/{Resources.Categories}";
        public const string Suppliers = $"{Base}/{Resources.Suppliers}";
        public const string Customers = $"{Base}/{Resources.Customers}";
        public const string Warehouses = $"{Base}/{Resources.Warehouses}";
        public const string Inbound = $"{Base}/{Resources.Inbound}";
    }
}

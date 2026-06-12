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
    }

    public static class Groups
    {
        public const string Skus = $"{Base}/{Resources.Skus}";
        public const string Products = $"{Base}/{Resources.Products}";
    }
}

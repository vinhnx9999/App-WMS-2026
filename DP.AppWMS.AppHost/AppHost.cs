var builder = DistributedApplication.CreateBuilder(args);

//var postgres = builder.AddPostgres("postgres").AddDatabase("Default", "wms_db");

var cache = builder.AddRedis("Redis").WithDataVolume("redis-data");

var apiService = builder.AddProject<Projects.DP_AppWMS_ApiService>("apiservice")
     .WithReference(cache)
     .WaitFor(cache)
     .WithHttpHealthCheck("/health");

builder.AddProject<Projects.DP_AppWMS_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddViteApp("react-spa", "../dp.appwms.spa")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("VITE_API_URL", apiService.GetEndpoint("https"))
    .WithExternalHttpEndpoints();

builder.Build().Run();

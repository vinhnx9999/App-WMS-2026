var builder = DistributedApplication.CreateBuilder(args);

//var postgres = builder.AddPostgres("postgres").AddDatabase("Default", "wms_db");

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.DP_AppWMS_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.DP_AppWMS_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
var db = builder.AddPostgres("postgres").AddDatabase("AlfabetPretDb");

var apiService = builder.AddProject<Projects.AlfabetPret_API>("api")
    .WithReference(cache)
    .WithReference(db);

var mobile = builder.AddNpmApp("mobile", "../AlfabetPret.App", "start")
    .WithReference(apiService)
    .WithEnvironment("EXPO_PUBLIC_API_URL", apiService.GetEndpoint("http"));

builder.Build().Run();

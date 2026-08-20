using k8s.KubeConfigModels;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// ---------------------------------------------------------------------------
// Strapi secrets (the keys Strapi requires to boot in production). Declared as
// parameters with default values so they flow into the generated .env file.
// ---------------------------------------------------------------------------
var appKeys = builder.AddParameter("strapi-app-keys", "fRf6ymSa1ylnCs/1afBDKQ==,X5xs4gviVUMufidv+K+NIA==,P/C6PxMYuBtfDwhqSXGeKw==,WhP1dad06zZ2KFHv12sg6w==");
var apiTokenSalt = builder.AddParameter("strapi-api-token-salt", "6e09nOW/HmyN0ljWylu/xg==");
var adminJwtSecret = builder.AddParameter("strapi-admin-jwt-secret", "LtcITDLbH5YPii65iSf8XA==", secret: true);
var transferTokenSalt = builder.AddParameter("strapi-transfer-token-salt", "e4/p83h7cF6/qKtDaZMtsw==");
var jwtSecret = builder.AddParameter("strapi-jwt-secret", "mQ5coPOakZozE/AGHOvveQ==", secret: true);
var encryptionKey = builder.AddParameter("strapi-encryption-key", "jaE81N8PoF2qVn0xczGK0w==", secret: true);

// Database credentials – declared explicitly so both Postgres and Strapi agree.
var dbUser = builder.AddParameter("postgres-username", "pretcms");
var dbPassword = builder.AddParameter("postgres-password", "pretpassword", secret: true);

// ---------------------------------------------------------------------------
// PostgreSQL with a persistent data volume.
// ---------------------------------------------------------------------------

var postgres = builder
    .AddPostgres(
        "postgres",
        userName: dbUser,
        password: dbPassword)
    .WithDataVolume("alfabetpret-pgdata")
    .WithBindMount(
        Path.Combine(
            builder.Environment.ContentRootPath,
            "postgres",
            "init"),
        "/docker-entrypoint-initdb.d")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "postgres";
        service.Restart = "unless-stopped";
    });

var db = postgres.AddDatabase(
    "AlfabetPretDb",
    databaseName: "AlfabetPretDb");

var apiService = builder.AddProject<Projects.AlfabetPret_API>("api")
    .WithReference(cache)
    .WithReference(db);

// ---------------------------------------------------------------------------
// Strapi CMS – built from the cms Dockerfile, backed by
// Postgres, exposed on port 1337.
// ---------------------------------------------------------------------------
var strapi = builder
    .AddDockerfile("Strapi", "../AlfabetPret.CMS")
    .WithHttpEndpoint(targetPort: 1337, port: 1337, name: "http")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WithReference(apiService)
    .WaitFor(db)
    .WaitFor(apiService)
    .WithEnvironment("HOST", "0.0.0.0")
    .WithEnvironment("PORT", "1337")
    .WithEnvironment("NODE_ENV", "production")
    .WithEnvironment("DATABASE_CLIENT", "postgres")
    .WithEnvironment("DATABASE_HOST", postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host))
    .WithEnvironment("DATABASE_PORT", postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.TargetPort))
    .WithEnvironment("DATABASE_NAME", "AlfabetPretDb")
    .WithEnvironment("DATABASE_USERNAME", dbUser)
    .WithEnvironment("DATABASE_PASSWORD", dbPassword)
    .WithEnvironment("DATABASE_SSL", "false")
    .WithEnvironment("APP_KEYS", appKeys)
    .WithEnvironment("API_TOKEN_SALT", apiTokenSalt)
    .WithEnvironment("ADMIN_JWT_SECRET", adminJwtSecret)
    .WithEnvironment("TRANSFER_TOKEN_SALT", transferTokenSalt)
    .WithEnvironment("JWT_SECRET", jwtSecret)
    .WithEnvironment("ENCRYPTION_KEY", encryptionKey)
    // Persist uploaded media. Strapi's local provider writes files to
    // public/uploads on the container filesystem; without a volume they are
    // lost whenever the container is recreated (leaving the DB pointing at
    // 404ing /uploads/* URLs).
    .WithVolume("cms-uploads", "/opt/app/public/uploads")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Restart = "unless-stopped";
    });

var mobile = builder.AddNpmApp("mobile", "../AlfabetPret.App", "start")
    .WithReference(apiService)
    .WithEnvironment("EXPO_PUBLIC_API_URL", apiService.GetEndpoint("http"));

builder.Build().Run();

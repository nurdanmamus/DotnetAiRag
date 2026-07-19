var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL — pgvector uzantılı imaj (RAG'daki vektör araması için gerekli).
// Aspire bunu otomatik bir Docker container olarak ayağa kaldırır.
var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg17")
    .WithDataVolume();

var appdb = postgres.AddDatabase("appdb");

// OpenAI anahtarını yapılandırmadan (user-secrets / env / launchSettings) okuyup API'ye iletiyoruz.
var openAiKey = builder.Configuration["OPEN_AI_KEY"];

builder.AddProject<Projects.App_API>("app-api")
    .WithReference(appdb)
    .WaitFor(appdb)
    .WithEnvironment("OPEN_AI_KEY", openAiKey);

builder.Build().Run();

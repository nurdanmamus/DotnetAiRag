using App.API.Data;
using App.API.Endpoints;
using App.API.Extensions;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// PostgreSQL + pgvector — "appdb" connection string'ini Aspire enjekte eder.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("appdb"),
        o => o.UseVector()));

// OpenAI chat + embedding + RAG servisleri
builder.Services.AddAiServices();

var app = builder.Build();

// Açılışta veritabanı şemasını otomatik uygula
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapDefaultEndpoints();

app.MapChatEndpoints();
app.MapDocumentEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

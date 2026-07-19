using App.API.Services;
using Microsoft.Extensions.AI;
using OpenAI;

namespace App.API.Extensions;

internal static class AiServiceCollectionExtensions
{
    /// <summary>
    /// OpenAI chat + embedding istemcilerini ve RAG servislerini kaydeder.
    /// </summary>
    internal static IServiceCollection AddAiServices(this IServiceCollection services)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPEN_AI_KEY")
                     ?? throw new InvalidOperationException("OPEN_AI_KEY ortam değişkenini ayarlayın.");

        var openAiClient = new OpenAIClient(apiKey);

        // Cevap üreten model
        services.AddChatClient(openAiClient.GetChatClient("gpt-4o").AsIChatClient())
            .UseFunctionInvocation()
            .UseLogging();

        // Metni vektöre çeviren embedding üreticisi
        services.AddEmbeddingGenerator(
            openAiClient.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator());

        // RAG servisleri
        services.AddSingleton<PdfProcessingService>();
        services.AddSingleton<EmbeddingService>();
        services.AddScoped<VectorSearchService>();
        services.AddScoped<RagService>();

        return services;
    }
}

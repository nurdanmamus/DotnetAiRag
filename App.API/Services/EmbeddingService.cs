using Microsoft.Extensions.AI;

namespace App.API.Services;

public class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var embedding = await embeddingGenerator.GenerateAsync(text);
        return embedding.Vector.ToArray();
    }

    public async Task<List<float[]>> GetEmbeddingsAsync(IEnumerable<string> texts)
    {
        var textList = texts.ToList();
        var result = new List<float[]>();

        // API sınırlarını aşmamak için 20'lik gruplar halinde işle
        const int batchSize = 20;
        for (var i = 0; i < textList.Count; i += batchSize)
        {
            var batch = textList.Skip(i).Take(batchSize).ToList();
            var embeddings = await embeddingGenerator.GenerateAsync(batch);

            foreach (var embedding in embeddings)
            {
                result.Add(embedding.Vector.ToArray());
            }
        }

        return result;
    }
}

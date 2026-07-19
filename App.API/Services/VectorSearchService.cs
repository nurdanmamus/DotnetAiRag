using App.API.Data;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace App.API.Services;

public class VectorSearchService(AppDbContext db)
{
    // Soru vektörüne en yakın (kosinüs mesafesi) parçaları bulur.
    public async Task<List<ChunkSearchResult>> SearchSimilarChunksAsync(float[] queryEmbedding, int topN = 10,
        int? documentId = null)
    {
        var queryVector = new Vector(queryEmbedding);

        return await db.DocumentChunks
            .Where(c => c.Embedding != null && (!documentId.HasValue || c.DocumentId == documentId))
            .Select(c => new
            {
                c.Id,
                c.DocumentId,
                c.Content,
                c.PageNumber,
                c.ChunkIndex,
                c.Document.OriginalFileName,
                // pgvector: CosineDistance -> PostgreSQL "<=>" operatörüne çevrilir
                Distance = c.Embedding!.CosineDistance(queryVector)
            })
            .OrderBy(c => c.Distance)
            .Take(topN)
            .Select(c => new ChunkSearchResult(
                c.Id,
                c.DocumentId,
                c.Content,
                c.PageNumber,
                c.ChunkIndex,
                c.OriginalFileName,
                c.Distance))
            .ToListAsync();
    }
}

public record ChunkSearchResult(
    int Id,
    int DocumentId,
    string Content,
    int PageNumber,
    int ChunkIndex,
    string DocumentName,
    double Relevance);

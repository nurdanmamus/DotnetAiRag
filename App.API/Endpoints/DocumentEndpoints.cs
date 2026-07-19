using App.API.Data;
using App.API.Services;
using Microsoft.AspNetCore.Mvc;
using Pgvector;

namespace App.API.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        group.MapPost("/upload", UploadDocument).DisableAntiforgery();
    }

    private static async Task<IResult> UploadDocument(
        IFormFile file,
        [FromServices] AppDbContext db,
        [FromServices] EmbeddingService embeddingService,
        [FromServices] PdfProcessingService pdfService,
        [FromServices] IWebHostEnvironment env)
    {
        if (file.Length == 0 || !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("Sadece PDF dosyaları yüklenebilir.");

        // Dosyayı diske kaydet (ContentRootPath altında uploads klasörü)
        var uploadsDir = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var savedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, savedFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // PDF'ten metin parçalarını çıkar
        List<(string Content, int PageNumber)> chunks;
        int pageCount;

        await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            (pageCount, chunks) = pdfService.ExtractChunks(stream);
        }

        if (chunks.Count == 0)
            return Results.BadRequest("PDF dosyasından metin çıkarılamadı.");

        // Document kaydını oluştur
        var document = new Document
        {
            FileName = savedFileName,
            OriginalFileName = file.FileName,
            UploadedAt = DateTime.UtcNow,
            PageCount = pageCount,
            TotalChunks = chunks.Count
        };

        db.Documents.Add(document);
        await db.SaveChangesAsync();

        // Parçaların embedding'lerini üret, sonra hepsini tek seferde kaydet
        var texts = chunks.Select(c => c.Content).ToList();
        var embeddings = await embeddingService.GetEmbeddingsAsync(texts);

        var chunkEntities = chunks.Select((chunk, chunkIndex) => new DocumentChunk
        {
            DocumentId = document.Id,
            ChunkIndex = chunkIndex,
            Content = chunk.Content,
            PageNumber = chunk.PageNumber,
            Embedding = new Vector(embeddings[chunkIndex])
        }).ToList();

        db.DocumentChunks.AddRange(chunkEntities);
        await db.SaveChangesAsync();

        return Results.Created($"/api/documents/{document.Id}",
            new UploadDocumentResponse(
                document.Id,
                document.OriginalFileName,
                document.PageCount,
                document.TotalChunks,
                document.UploadedAt));
    }
}

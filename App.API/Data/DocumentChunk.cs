using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace App.API.Data;

public class DocumentChunk
{
    public int Id { get; set; }

    public int ChunkIndex { get; set; }
    public string Content { get; set; } = default!;
    public int PageNumber { get; set; }

    // Parçanın (chunk) embedding'i — text-embedding-3-small => 1536 boyut.
    [Column(TypeName = "vector(1536)")]
    public Vector? Embedding { get; set; }

    public int DocumentId { get; set; }
    public Document Document { get; set; } = default!;
}

namespace App.API.Endpoints;

public record UploadDocumentResponse(
    int Id,
    string FileName,
    int PageCount,
    int TotalChunks,
    DateTime UploadedAt);

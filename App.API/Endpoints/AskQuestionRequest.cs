namespace App.API.Endpoints;

public record AskQuestionRequest(string Question, int? DocumentId = null);

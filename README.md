# dotnet-ai-rag

A **RAG (Retrieval-Augmented Generation)** document Q&A app built with .NET + OpenAI.
Upload PDFs; the app splits them into chunks, embeds each chunk, and stores them in
PostgreSQL + **pgvector**. When you ask a question, it embeds the question, retrieves the
most relevant chunks by cosine distance, and asks an LLM to answer **using only those
sources** — returning the answer plus its source references. .NET Aspire brings everything
up with a single command.

## How RAG works here

```
PDF ──► extract text (PdfPig) ──► chunk (~1000 chars, 200 overlap)
     ──► embed each chunk (text-embedding-3-small) ──► store in pgvector

Question ──► embed ──► vector search (top 10 nearest chunks)
         ──► build context ──► LLM (gpt-4o) ──► answer + sources
```

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/api/documents/upload` | Upload a PDF (multipart form field `file`) |
| `POST` | `/api/chat/ask` | Ask a question; optional `documentId` to scope to one doc |

## Projects

| Project | Purpose |
|---------|---------|
| `App.API` | Web API — upload + RAG Q&A |
| `DotnetAiRag.AppHost` | .NET Aspire orchestrator (Postgres container + API) |
| `DotnetAiRag.ServiceDefaults` | Shared Aspire defaults |

## Tech stack

- **.NET 10** minimal API + **.NET Aspire** (orchestration, runs the Postgres container)
- **Microsoft.Extensions.AI** — provider-agnostic AI abstractions (`IChatClient`, `IEmbeddingGenerator`), plus midd
leware like `UseFunctionInvocation` / `UseLogging`. The app talks to these interfaces, so the AI provider can be sw
apped by changing only the registration.
- **Microsoft.Extensions.AI.OpenAI** + **OpenAI** SDK — OpenAI as the underlying provider (`gpt-4o`, `text-embeddin
g-3-small`)
- **PostgreSQL + pgvector** via EF Core (Npgsql) — chunk embedding storage & similarity search
- **UglyToad.PdfPig** — PDF text extraction


## Requirements

- .NET 10 SDK
- Docker (Aspire starts Postgres as a container)
- An **OpenAI API key** (https://platform.openai.com/api-keys — paid, add some credit)

## Getting started

### 1) Provide your OpenAI key

Add it as an environment variable named `OPEN_AI_KEY`. When running from Rider, set it in the
**Run configuration** (or the AppHost's launch profile). The AppHost forwards it to the API and
reads it via configuration (env var / user-secrets).

> The key is never hardcoded and `launchSettings.json` is git-ignored so it won't reach GitHub.

### 2) Run (Aspire)

```bash
dotnet run --project DotnetAiRag.AppHost
```

The Aspire dashboard opens. `postgres` and `app-api` should both be **Running**; the database
schema (migrations) is applied automatically on startup.

### 3) Upload a PDF and ask

Easiest with **Postman**:

1. **Upload** — `POST http://<app-api-host>/api/documents/upload`
   - Body → **form-data**, key = `file`, type = **File**, choose a `.pdf`. Send.
   - Response includes the document `Id`, page count, and chunk count.
2. **Ask** — `POST http://<app-api-host>/api/chat/ask`
   - Body → **raw / JSON**:
     ```json
     { "question": "What is this document about?" }
     ```
   - Optionally add `"documentId": 1` to restrict the search to one document.
   - Response: the answer plus `sources` (document name, page, relevance).

See `App.API/App.API.http` for ready-made requests.

## PostgreSQL notes

- Chunk vector column: `vector(1536)` (pgvector); chunk text: `text`.
- Similarity ranking: `Embedding <=> queryVector` (cosine distance, `CosineDistance` in EF).
- Migrations apply automatically on startup (`Program.cs` → `db.Database.Migrate()`).

## Tuning ideas

- Chunk size / overlap: `PdfProcessingService` (`MaxChunkSize`, `OverlapSize`).
- Number of retrieved chunks: `RagService` (`topN`).
- System prompt / answer language: `RagService`.

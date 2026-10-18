using System.Security.Claims;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ContosoDashboard.Endpoints;

public static class DocumentContentEndpoints
{
    private static readonly HashSet<string> PreviewContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    public static IEndpointRouteBuilder MapDocumentContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/documents/{documentId:int}/content", HandleHttpAsync)
            .RequireAuthorization();
        return endpoints;
    }

    public static async Task<IResult> GetContentAsync(
        ClaimsPrincipal user,
        int documentId,
        string? disposition,
        IDocumentService documentService,
        IFileStorageService fileStorage,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId) || userId <= 0)
        {
            return TypedResults.Unauthorized();
        }

        var document = await documentService.GetAccessibleAsync(documentId, userId, cancellationToken);
        if (document == null || document.SafetyStatus != DocumentSafetyStatus.Clean)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Document not found." });
        }

        var requestedDisposition = string.IsNullOrWhiteSpace(disposition) ? "attachment" : disposition;
        if (requestedDisposition is not ("inline" or "attachment"))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Unsupported content disposition." });
        }

        if (requestedDisposition == "inline" && !PreviewContentTypes.Contains(document.ContentType))
        {
            return TypedResults.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        try
        {
            var content = await fileStorage.OpenReadAsync(document.StorageKey, cancellationToken);
            if (requestedDisposition == "attachment")
            {
                await documentService.RecordActivityAsync(document.DocumentId, userId, DocumentActivityType.Download, cancellationToken);
            }
            var safeFileName = Path.GetFileName(document.OriginalFileName.Replace('\\', '/'));
            return TypedResults.Stream(
                content,
                document.ContentType,
                fileDownloadName: requestedDisposition == "attachment" ? safeFileName : null,
                enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Document not found." });
        }
        catch (DirectoryNotFoundException)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Document not found." });
        }
        catch (IOException)
        {
            return TypedResults.Problem("The document content is temporarily unavailable.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> HandleHttpAsync(
        HttpContext context,
        int documentId,
        string? disposition,
        IDocumentService documentService,
        IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (disposition == "inline")
        {
            context.Response.Headers["Content-Disposition"] = "inline";
        }

        return await GetContentAsync(context.User, documentId, disposition, documentService, fileStorage, cancellationToken);
    }
}
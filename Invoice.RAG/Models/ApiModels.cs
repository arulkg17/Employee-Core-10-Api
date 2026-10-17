namespace Invoice.RAG.Models;

public sealed record ApiResult<T>(bool Success, string Message, T? Data = default);

/// <summary>Thrown for expected problems; the global handler turns it into the given HTTP status.</summary>
public sealed class RagException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}


using System.Text.Json.Nodes;

namespace Invoice.Model;

public class ApiTaskResult
{
    public string ApiName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public double ExecutionTimeInSeconds { get; set; }

    public string Message { get; set; } = string.Empty;

    public int? HttpStatusCode { get; set; }

    // Supports JSON objects, arrays, and null
    public JsonNode? Response { get; set; }
}

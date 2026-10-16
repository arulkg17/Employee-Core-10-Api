using ModelContextProtocol;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace IMS.MCP.Server.Services;

/// <summary>
/// One place for: token resolution, calling the Invoice CoreAPI, and turning HTTP
/// failures into messages the AI can read (McpException).
/// </summary>
public sealed class InvoiceApiClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<InvoiceApiClient> logger)
{
    public const string TokenHelp =
        "Optional JWT from POST /api/v1/Login. Leave empty if the MCP client already sends an Authorization header.";

    public async Task<string> GetAsync(
        string relativeUrl, string? accessToken, CancellationToken ct)
    {
        var token = ResolveToken(accessToken);
        var client = httpClientFactory.CreateClient("InvoiceApi");

        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Invoice API unreachable for {Url}", relativeUrl);
            throw new McpException(
                "Could not reach the Invoice API. Check that CoreAPI is running on the configured BaseUrl.");
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
                return body;

            // Full body goes to the server log only; the AI gets a safe summary
            // (the API puts ex.Message in Error.Details, which we don't forward).
            logger.LogWarning("Invoice API {Status} for {Url}: {Body}",
                (int)response.StatusCode, relativeUrl, body);

            throw new McpException(response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "Invoice API returned 401: token missing, expired or invalid. Log in again via POST /api/v1/Login.",
                HttpStatusCode.Forbidden =>
                    "Invoice API returned 403: this user is not allowed to access this resource.",
                HttpStatusCode.NotFound =>
                    "Invoice API returned 404: the record was not found.",
                _ =>
                    $"Invoice API returned HTTP {(int)response.StatusCode}. See server logs for details."
            });
        }
    }

    /// <summary>Builds "path?a=1&amp;b=x", skipping null/blank values and URL-encoding the rest.</summary>
    public static string WithQuery(string path, params (string Key, object? Value)[] pairs)
    {
        var parts = pairs
            .Select(p => (p.Key, Text: Convert.ToString(p.Value, CultureInfo.InvariantCulture)))
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Text!)}");

        var query = string.Join('&', parts);
        return query.Length == 0 ? path : $"{path}?{query}";
    }

    /// <summary>Keeps AI-driven paging sane so one call can't pull thousands of rows into the context.</summary>
    public static (int Page, int Size) ClampPaging(int pageNumber, int pageSize)
        => (Math.Max(1, pageNumber), Math.Clamp(pageSize, 1, 50));

    /// <summary>Prefers the Authorization header from the MCP client; falls back to the tool argument.</summary>
    private string ResolveToken(string? accessToken)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(header) &&
            header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return header["Bearer ".Length..].Trim();
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
            return accessToken.Trim();

        throw new McpException(
            "No access token. Send an 'Authorization: Bearer <jwt>' header or pass accessToken.");
    }
}

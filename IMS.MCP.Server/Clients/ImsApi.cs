using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IMS.MCP.Server.Clients;

/// <summary>An expected problem (not found, token rejected, API down). Tools turn it into {"error": "..."} for the model.</summary>
public sealed class ApiToolException(string message) : Exception(message);

/// <summary>
/// One place for the two back ends the vendor tools use:
///   "CoreApi" = Invoice CoreAPI   (structured data: vendors, purchase orders; reads the Accounts database through DAL/BAL)
///   "RagApi"  = Invoice.RAG API   (unstructured data: agreement text, terms; reads the InvoiceRag database)
/// Both calls carry the caller's JWT (see ForwardAuthHandler). The tools never connect to SQL Server themselves.
/// </summary>
public sealed class ImsApi(IHttpClientFactory factory, IConfiguration config)
{
    public const string Core = "CoreApi";
    public const string Rag = "RagApi";

    /// <summary>Route templates can be overridden in appsettings under Ims:Paths:{key}.</summary>
    public string Path(string key, string fallback) => config[$"Ims:Paths:{key}"] ?? fallback;

    public double MinScore => double.TryParse(config["Ims:MinScore"], out var v) ? v : 0.25;

    public Task<JsonElement?> GetAsync(string client, string url, bool allowNotFound, CancellationToken ct)
        => SendAsync(client, new HttpRequestMessage(HttpMethod.Get, url), allowNotFound, ct);

    public Task<JsonElement?> PostAsync(string client, string url, object body, CancellationToken ct)
        => SendAsync(client, new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) }, false, ct);

    private async Task<JsonElement?> SendAsync(string client, HttpRequestMessage request, bool allowNotFound, CancellationToken ct)
    {
        var http = factory.CreateClient(client);
        var label = client == Core ? "CoreAPI" : "Invoice.RAG API";

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException)
        {
            throw new ApiToolException($"{label} is not reachable. Check that it is running.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ApiToolException($"{label} took too long to answer.");
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound && allowNotFound) return null;
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new ApiToolException($"{label} rejected the token (401). The MCP client must send a valid Authorization header.");
            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new ApiToolException($"{label} says this user is not allowed to read this data (403).");

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(ct);
                if (detail.Length > 300) detail = detail[..300];
                throw new ApiToolException($"{label} returned HTTP {(int)response.StatusCode}. {detail}");
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array) return root.Clone();
            return Js.Find(root, "data")?.Clone();
        }
    }
}

/// <summary>Runs a tool body and returns JSON text. Expected problems come back as {"error": "..."} instead of failing the call.</summary>
public static class ToolRunner
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<string> RunAsync(Func<Task<object>> work)
    {
        try
        {
            return JsonSerializer.Serialize(await work(), Options);
        }
        catch (ApiToolException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, Options);
        }
    }
}

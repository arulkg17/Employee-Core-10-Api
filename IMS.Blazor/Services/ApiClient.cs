using System.Net;
using System.Text;
using System.Text.Json;
using IMS.Blazor.Models;

namespace IMS.Blazor.Services;

/// <summary>A business or network problem; the message is safe to show to the user.</summary>
public class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}

/// <summary>
/// What came back from the API, in one shape. The IMS API answers in a few different styles
/// (ApiResponse wrapper, plain true/false, plain object) and this class understands all of them.
/// </summary>
public class ApiResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = "";

    /// <summary>The "data" part of ApiResponse (or the whole body when there is no wrapper).</summary>
    public JsonElement? Data { get; init; }

    public int? TotalRecords { get; init; }

    public string RawText { get; init; } = "";

    public static ApiResult Fail(string message) => new() { Success = false, Message = message };

    public string MessageOr(string fallback) =>
        string.IsNullOrWhiteSpace(Message) ? fallback : Message;

    /// <summary>The new id returned by a Create call (when the API sends one).</summary>
    public int? NewId =>
        Data is { ValueKind: JsonValueKind.Number } d && d.TryGetInt32(out var id) ? id : null;

    /// <summary>The data as an object (GetById, login ...).</summary>
    public T? As<T>()
    {
        if (Data is { } d && d.ValueKind != JsonValueKind.Null && d.ValueKind != JsonValueKind.Undefined)
        {
            return d.Deserialize<T>(ApiClient.JsonOptions);
        }

        return default;
    }

    /// <summary>GetAll: the list directly, or { data: [...] }.</summary>
    public List<T> AsList<T>()
    {
        if (Data is not { } d)
        {
            return new List<T>();
        }

        if (d.ValueKind == JsonValueKind.Array)
        {
            return d.Deserialize<List<T>>(ApiClient.JsonOptions) ?? new List<T>();
        }

        if (d.ValueKind == JsonValueKind.Object
            && d.TryGetProperty("data", out var inner)
            && inner.ValueKind == JsonValueKind.Array)
        {
            return inner.Deserialize<List<T>>(ApiClient.JsonOptions) ?? new List<T>();
        }

        return new List<T>();
    }

    /// <summary>
    /// GetAllPaged comes in two shapes in this API:
    ///   older : { data: [...], totalRecords }
    ///   newer : { data: { data: [...], totalRecords } }
    /// </summary>
    public PagedResult<T> AsPaged<T>()
    {
        if (Data is not { } d)
        {
            return new PagedResult<T>();
        }

        if (d.ValueKind == JsonValueKind.Array)
        {
            var items = d.Deserialize<List<T>>(ApiClient.JsonOptions) ?? new List<T>();

            return new PagedResult<T> { Data = items, TotalRecords = TotalRecords ?? items.Count };
        }

        if (d.ValueKind == JsonValueKind.Object
            && d.TryGetProperty("data", out var inner)
            && inner.ValueKind == JsonValueKind.Array)
        {
            var items = inner.Deserialize<List<T>>(ApiClient.JsonOptions) ?? new List<T>();
            var total = d.TryGetProperty("totalRecords", out var t) && t.TryGetInt32(out var n)
                ? n
                : TotalRecords ?? items.Count;

            return new PagedResult<T> { Data = items, TotalRecords = total };
        }

        return new PagedResult<T>();
    }
}

/// <summary>The one class that talks HTTP to the IMS API. Every other service uses it.</summary>
public class ApiClient
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public ApiClient(HttpClient http) => _http = http;

    public Task<ApiResult> GetAsync(string url, IDictionary<string, string?>? query = null) =>
        SendAsync(HttpMethod.Get, BuildUrl(url, query), null);

    public Task<ApiResult> PostAsync(string url, object? body = null) =>
        SendAsync(HttpMethod.Post, url, body ?? new { });

    public Task<ApiResult> PutAsync(string url, object body) =>
        SendAsync(HttpMethod.Put, url, body);

    public Task<ApiResult> DeleteAsync(string url) =>
        SendAsync(HttpMethod.Delete, url, null);

    /// <summary>Downloads a file (the PDFs).</summary>
    public async Task<byte[]> GetBytesAsync(string url)
    {
        try
        {
            using var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();

                throw new ApiException(Parse(response, text).MessageOr("Unable to load the file."));
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (HttpRequestException)
        {
            throw new ApiException("Cannot reach the API. Is the backend running?");
        }
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, body.GetType(), JsonOptions);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();

            return Parse(response, text);
        }
        catch (HttpRequestException)
        {
            return ApiResult.Fail("Cannot reach the API. Is the backend running?");
        }
        catch (TaskCanceledException)
        {
            return ApiResult.Fail("The API did not answer in time.");
        }
    }

    private static string BuildUrl(string url, IDictionary<string, string?>? query)
    {
        if (query is null)
        {
            return url;
        }

        // only the filters the user filled in
        var parts = query
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!.Trim())}")
            .ToList();

        return parts.Count == 0 ? url : $"{url}?{string.Join("&", parts)}";
    }

    private static ApiResult Parse(HttpResponseMessage response, string text)
    {
        var success = response.IsSuccessStatusCode;
        var message = "";
        JsonElement? data = null;
        int? totalRecords = null;

        JsonElement root = default;
        var hasJson = false;

        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var document = JsonDocument.Parse(text);

                root = document.RootElement.Clone();
                hasJson = true;
            }
            catch (JsonException)
            {
                // not JSON (plain text): RawText still has it
            }
        }

        if (hasJson)
        {
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                {
                    message = m.GetString() ?? "";
                }

                if (root.TryGetProperty("success", out var s)
                    && (s.ValueKind == JsonValueKind.True || s.ValueKind == JsonValueKind.False))
                {
                    success = success && s.GetBoolean();
                }

                if (string.IsNullOrEmpty(message)
                    && root.TryGetProperty("error", out var e)
                    && e.ValueKind == JsonValueKind.Object
                    && e.TryGetProperty("details", out var details)
                    && details.ValueKind == JsonValueKind.String)
                {
                    message = details.GetString() ?? "";
                }

                if (root.TryGetProperty("totalRecords", out var tr) && tr.TryGetInt32(out var total))
                {
                    totalRecords = total;
                }

                data = root.TryGetProperty("data", out var inner) ? inner : root;
            }
            else
            {
                if (root.ValueKind == JsonValueKind.True || root.ValueKind == JsonValueKind.False)
                {
                    success = success && root.GetBoolean();
                }

                data = root; // a number (new id), an array, a string ...
            }
        }

        if (!response.IsSuccessStatusCode && string.IsNullOrEmpty(message))
        {
            message = response.StatusCode == HttpStatusCode.Unauthorized
                ? "Your session has expired. Please log in again."
                : $"The request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
        }

        return new ApiResult
        {
            Success = success,
            Message = message,
            Data = data,
            TotalRecords = totalRecords,
            RawText = text,
        };
    }
}

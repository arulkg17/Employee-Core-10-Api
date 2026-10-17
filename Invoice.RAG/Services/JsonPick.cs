using System.Globalization;
using System.Text.Json;

namespace Invoice.RAG.Services;

/// <summary>Tolerant reading of CoreAPI JSON: case-insensitive property lookup with alternative names.</summary>
public static class JsonPick
{
    public static JsonElement? Find(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;

        foreach (var name in names)
            foreach (var p in element.EnumerateObject())
                if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind != JsonValueKind.Null)
                    return p.Value;

        return null;
    }

    public static string? Str(JsonElement element, params string[] names)
    {
        var v = Find(element, names);
        if (v is null) return null;
        return v.Value.ValueKind == JsonValueKind.String ? v.Value.GetString() : v.Value.ToString();
    }

    public static int? Int(JsonElement element, params string[] names)
    {
        var v = Find(element, names);
        if (v is null) return null;
        if (v.Value.ValueKind == JsonValueKind.Number && v.Value.TryGetInt32(out var n)) return n;
        return int.TryParse(v.Value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : null;
    }

    public static decimal? Dec(JsonElement element, params string[] names)
    {
        var v = Find(element, names);
        if (v is null) return null;
        if (v.Value.ValueKind == JsonValueKind.Number && v.Value.TryGetDecimal(out var n)) return n;
        return decimal.TryParse(v.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s) ? s : null;
    }

    public static DateOnly? Date(JsonElement element, params string[] names)
    {
        var v = Find(element, names);
        if (v is null || v.Value.ValueKind != JsonValueKind.String) return null;
        return DateTime.TryParse(v.Value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? DateOnly.FromDateTime(d)
            : null;
    }

    /// <summary>Rows of a list response: either "data": [ ... ] or "data": { "data": [ ... ], "totalRecords": n }.</summary>
    public static IEnumerable<JsonElement> Rows(JsonElement data)
    {
        if (data.ValueKind == JsonValueKind.Array) return data.EnumerateArray();

        var inner = Find(data, "data");
        if (inner is { ValueKind: JsonValueKind.Array })
            return inner.Value.EnumerateArray();

        return Enumerable.Empty<JsonElement>();
    }
}

using System.Security.Claims;
using System.Text.Json;

namespace IMS.Blazor.Services;

/// <summary>Reads the claims out of a JWT (no signature check: the API does that).</summary>
public static class JwtParser
{
    public static List<Claim>? ParseClaims(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');

            if (parts.Length != 3)
            {
                return null;
            }

            using var document = JsonDocument.Parse(DecodeBase64Url(parts[1]));
            var claims = new List<Claim>();

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(property.Name, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(property.Name, property.Value.ToString()));
                }
            }

            claims.Add(new Claim(ClaimTypes.Name, DisplayName(claims)));

            return claims;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }

    /// <summary>displayName, else "first last", else the user name.</summary>
    private static string DisplayName(List<Claim> claims)
    {
        string? Find(string type) => claims.FirstOrDefault(c => c.Type == type)?.Value;

        var display = Find("displayName");

        if (!string.IsNullOrWhiteSpace(display))
        {
            return display;
        }

        var full = $"{Find("given_name")} {Find("family_name")}".Trim();

        if (full.Length > 0)
        {
            return full;
        }

        return Find("unique_name") ?? Find("name") ?? "user";
    }

    /// <summary>The "exp" claim (seconds since 1970) as a date, or null when the token has none.</summary>
    public static DateTimeOffset? GetExpiry(IEnumerable<Claim> claims)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;

        return long.TryParse(exp, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}

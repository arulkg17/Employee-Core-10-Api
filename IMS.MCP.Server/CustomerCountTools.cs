
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;

[McpServerToolType]
public static class CustomerCountTools
{
    private static readonly HttpClient HttpClient = new();

    private const string CustomerApiUrl =
        "http://localhost:5063/api/v1/Customer/GetAll";

    private static async Task<(int Active, int Inactive)>
        GetCustomerCountsAsync()
    {
        var token = Environment.GetEnvironmentVariable(
            "IMS_API_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "IMS_API_TOKEN is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get, CustomerApiUrl);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await HttpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.GetProperty("success").GetBoolean())
        {
            throw new InvalidOperationException(
                "The Customer API reported a failure.");
        }

        var customers = root.GetProperty("data");

        int active = 0;
        int inactive = 0;

        foreach (var customer in customers.EnumerateArray())
        {
            bool isActive = customer
                .GetProperty("isActive").GetBoolean();

            bool isDeleted = customer
                .GetProperty("isDeleted").GetBoolean();

            if (isActive && !isDeleted)
            {
                active++;
            }
            else
            {
                inactive++;
            }
        }

        return (active, inactive);
    }

    [McpServerTool]
    [Description(
        "Returns the count of active, non-deleted IMS customers.")]
    public static async Task<string> GetActiveCustomerCount()
    {
        var counts = await GetCustomerCountsAsync();

        return $"Active, non-deleted customers: {counts.Active}";
    }

    [McpServerTool]
    [Description(
        "Returns the count of inactive or deleted IMS customers.")]
    public static async Task<string> GetInactiveCustomerCount()
    {
        var counts = await GetCustomerCountsAsync();

        return $"Inactive or deleted customers: {counts.Inactive}";
    }
}
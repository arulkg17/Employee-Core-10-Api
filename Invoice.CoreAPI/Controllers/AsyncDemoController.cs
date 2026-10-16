using Invoice.Model;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace Invoice.CoreAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AsyncDemoController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AsyncDemoController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    private async Task<ApiTaskResult> CallApiAsync(
     string apiName,
     string apiPath,
     CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("InvoiceApi");

            using var request = new HttpRequestMessage(
                HttpMethod.Get, apiPath);

            // Forward the caller's JWT if the API requires authorization.
            var authorization =
                Request.Headers.Authorization.ToString();

            if (!string.IsNullOrWhiteSpace(authorization))
            {
                request.Headers.TryAddWithoutValidation(
                    "Authorization", authorization);
            }

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken);

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            // Convert the response body from JSON text to a JSON value.
            JsonNode? responseObject = null;

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    responseObject = JsonNode.Parse(responseBody);
                }
                catch (JsonException)
                {
                    // Preserve a non-JSON response as a JSON string.
                    responseObject = JsonValue.Create(responseBody);
                }
            }

            stopwatch.Stop();

            return new ApiTaskResult
            {
                ApiName = apiName,
                Status = response.IsSuccessStatusCode ? "Pass" : "Fail",
                HttpStatusCode = (int)response.StatusCode,
                ExecutionTimeInSeconds =
                    stopwatch.Elapsed.TotalSeconds,
                Message = response.IsSuccessStatusCode
                    ? $"{apiName} API succeeded."
                    : $"{apiName} API returned {response.StatusCode}.",
                Response = responseObject
            };
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            return new ApiTaskResult
            {
                ApiName = apiName,
                Status = "Fail",
                ExecutionTimeInSeconds =
                    stopwatch.Elapsed.TotalSeconds,
                Message = "The API request timed out."
            };
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Preserve cancellation of the incoming request.
            throw;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();

            return new ApiTaskResult
            {
                ApiName = apiName,
                Status = "Fail",
                ExecutionTimeInSeconds =
                    stopwatch.Elapsed.TotalSeconds,
                Message = $"HTTP request failed: {ex.Message}"
            };
        }
    }

    [HttpGet("run-all")]
    public async Task<IActionResult> RunAllAsync(
        CancellationToken cancellationToken)
    {
        // Configure ApiBaseUrl in appsettings.json.
        var baseUrl = _configuration["ApiBaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl) ||
            !Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            return Problem(
                title: "Invalid API configuration",
                detail: "Configure ApiBaseUrl with an absolute URL.");
        }

        var apiRequests = new[]
        {
            (Name: "Category",   Path: "api/v1/Category/GetAll"),
            (Name: "Itemmaster", Path: "api/v1/Itemmaster/GetAll"),
            (Name: "Customer",   Path: "api/v1/Customer/GetAll"),
            (Name: "Vendor",     Path: "api/v1/Vendor/GetAll"),
            (Name: "User",       Path: "api/v1/User")
        };

        //var client = _httpClientFactory.CreateClient("InvoiceApi");
        //client.BaseAddress = new Uri(
        //    baseUrl.TrimEnd('/') + "/");

        // Calling each async method starts its HTTP request.
        var tasks = apiRequests.Select(api =>
            CallApiAsync(
                api.Name,
                api.Path,
                cancellationToken));

        // Wait until all five API calls have completed.
        var results = await Task.WhenAll(tasks);

        return Ok(new
        {
            Total = results.Length,
            Passed = results.Count(x => x.Status == "Pass"),
            Failed = results.Count(x => x.Status == "Fail"),
            TotalExecutionTimeInSeconds =
                results.Max(x => x.ExecutionTimeInSeconds),
            Results = results
        });
    }
}

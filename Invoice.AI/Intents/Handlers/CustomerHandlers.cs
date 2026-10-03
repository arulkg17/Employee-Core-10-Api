using Invoice.AI.Abstractions;
using Invoice.AI.Models;

namespace Invoice.AI.Intents.Handlers;

public sealed class CustomerCountHandler : IIntentHandler
{
    private readonly IMasterDataQueryService _data;
    public CustomerCountHandler(IMasterDataQueryService data) => _data = data;

    public string IntentName => IntentNames.CustomerCount;

    public string Description => "Total number of customers (no city is mentioned).";

    public async Task<IntentExecutionResult> HandleAsync(IntentArguments args, CancellationToken ct)
    {
        var count = await _data.CountCustomersAsync(args.IsActive, ct);

        return IntentExecutionResult.Ok(
            IntentName,
            AnswerText.Count(count, args.IsActive, "customer", "customers"),
            new { isActive = args.IsActive, count });
    }
}

public sealed class CustomerCountByCityHandler : IIntentHandler
{
    private readonly IMasterDataQueryService _data;
    public CustomerCountByCityHandler(IMasterDataQueryService data) => _data = data;

    public string IntentName => IntentNames.CustomerCountByCity;

    public string Description => "Number of customers in ONE named city. Needs city.";

    public async Task<IntentExecutionResult> HandleAsync(IntentArguments args, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(args.City))
            return IntentExecutionResult.Fail(IntentName, "I could not identify the city from your question.");

        var count = await _data.CountCustomersByCityAsync(args.City, args.IsActive, ct);

        return IntentExecutionResult.Ok(
            IntentName,
            AnswerText.Count(count, args.IsActive, "customer", "customers", $"in {args.City}"),
            new { city = args.City, isActive = args.IsActive, count });
    }
}

public sealed class CustomerCountAllCitiesHandler : IIntentHandler
{
    private readonly IMasterDataQueryService _data;
    public CustomerCountAllCitiesHandler(IMasterDataQueryService data) => _data = data;

    public string IntentName => IntentNames.CustomerCountAllCities;

    public string Description =>
        "Customer counts for EVERY city (city-wise breakdown, 'all cities', 'each city').";

    public async Task<IntentExecutionResult> HandleAsync(IntentArguments args, CancellationToken ct)
    {
        var rows = await _data.CountCustomersGroupedByCityAsync(args.IsActive, ct);

        if (rows.Count == 0)
            return IntentExecutionResult.Ok(IntentName, "No customers were found.", rows);

        var title = args.IsActive switch
        {
            true => "Active customers by city",
            false => "Inactive customers by city",
            _ => "Customers by city"
        };

        var list = string.Join(", ", rows.Select(r => $"{r.City} {r.Count}"));

        return IntentExecutionResult.Ok(IntentName, $"{title}: {list}.", rows);
    }
}

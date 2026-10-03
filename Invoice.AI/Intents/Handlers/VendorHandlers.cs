using Invoice.AI.Abstractions;
using Invoice.AI.Models;

namespace Invoice.AI.Intents.Handlers;

public sealed class VendorCountHandler : IIntentHandler
{
    private readonly IMasterDataQueryService _data;
    public VendorCountHandler(IMasterDataQueryService data) => _data = data;

    public string IntentName => IntentNames.VendorCount;

    public string Description => "Total number of vendors / suppliers.";

    public async Task<IntentExecutionResult> HandleAsync(IntentArguments args, CancellationToken ct)
    {
        var count = await _data.CountVendorsAsync(args.IsActive, ct);

        return IntentExecutionResult.Ok(
            IntentName,
            AnswerText.Count(count, args.IsActive, "vendor", "vendors"),
            new { isActive = args.IsActive, count });
    }
}

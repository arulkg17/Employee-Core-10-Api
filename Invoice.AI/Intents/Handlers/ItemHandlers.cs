using Invoice.AI.Abstractions;
using Invoice.AI.Models;

namespace Invoice.AI.Intents.Handlers;

public sealed class ItemCountHandler : IIntentHandler
{
    private readonly IMasterDataQueryService _data;
    public ItemCountHandler(IMasterDataQueryService data) => _data = data;

    public string IntentName => IntentNames.ItemCount;

    public string Description =>
        "Total number of items/products in the item master across ALL categories " +
        "(no category is mentioned).";

    public async Task<IntentExecutionResult> HandleAsync(IntentArguments args, CancellationToken ct)
    {
        var count = await _data.CountItemsAsync(args.IsActive, ct);

        return IntentExecutionResult.Ok(
            IntentName,
            AnswerText.Count(count, args.IsActive, "item", "items"),
            new { isActive = args.IsActive, count });
    }
}

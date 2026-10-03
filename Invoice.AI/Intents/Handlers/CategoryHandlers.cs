using Invoice.AI.Abstractions;
using Invoice.AI.Models;

namespace Invoice.AI.Intents.Handlers;

public sealed class CategoryItemCountHandler : IIntentHandler
{
    private readonly IMasterDataQueryService _data;
    public CategoryItemCountHandler(IMasterDataQueryService data) => _data = data;

    public string IntentName => IntentNames.CategoryItemCount;

    public string Description =>
        "Number of items/products that belong to ONE named category. Needs categoryName. " +
        "activeFilter applies to the items.";

    public async Task<IntentExecutionResult> HandleAsync(IntentArguments args, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(args.CategoryName))
            return IntentExecutionResult.Fail(IntentName, "I could not identify the category from your question.");

        var count = await _data.CountItemsInCategoryAsync(args.CategoryName, args.IsActive, ct);

        if (count is null)
            return IntentExecutionResult.Fail(IntentName, $"I could not find the category '{args.CategoryName}'.");

        return IntentExecutionResult.Ok(
            IntentName,
            AnswerText.Count(count.Value, args.IsActive, "item", "items", $"in the {args.CategoryName} category"),
            new { category = args.CategoryName, isActive = args.IsActive, count = count.Value });
    }
}

public sealed class CategoryCountHandler : IIntentHandler
{
    private readonly IMasterDataQueryService _data;
    public CategoryCountHandler(IMasterDataQueryService data) => _data = data;

    public string IntentName => IntentNames.CategoryCount;

    public string Description => "Number of categories themselves (NOT the items inside them).";

    public async Task<IntentExecutionResult> HandleAsync(IntentArguments args, CancellationToken ct)
    {
        var count = await _data.CountCategoriesAsync(args.IsActive, ct);

        return IntentExecutionResult.Ok(
            IntentName,
            AnswerText.Count(count, args.IsActive, "category", "categories"),
            new { isActive = args.IsActive, count });
    }
}

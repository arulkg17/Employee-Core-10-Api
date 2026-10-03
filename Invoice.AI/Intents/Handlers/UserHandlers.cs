using Invoice.AI.Abstractions;
using Invoice.AI.Models;

namespace Invoice.AI.Intents.Handlers;

public sealed class UserCountHandler : IIntentHandler
{
    private readonly IMasterDataQueryService _data;
    public UserCountHandler(IMasterDataQueryService data) => _data = data;

    public string IntentName => IntentNames.UserCount;

    public string Description => "Total number of application users (people who can log in).";

    public async Task<IntentExecutionResult> HandleAsync(IntentArguments args, CancellationToken ct)
    {
        var count = await _data.CountUsersAsync(args.IsActive, ct);

        return IntentExecutionResult.Ok(
            IntentName,
            AnswerText.Count(count, args.IsActive, "user", "users"),
            new { isActive = args.IsActive, count });
    }
}

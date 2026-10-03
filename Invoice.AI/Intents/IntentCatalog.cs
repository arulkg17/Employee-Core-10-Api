using System.Diagnostics.CodeAnalysis;
using Invoice.AI.Abstractions;

namespace Invoice.AI.Intents;

/// <summary>Registry of every IIntentHandler that was registered in DI.</summary>
public sealed class IntentCatalog
{
    private readonly Dictionary<string, IIntentHandler> _handlers;

    public IntentCatalog(IEnumerable<IIntentHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.IntentName, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<IIntentHandler> Handlers => _handlers.Values;

    public IReadOnlyCollection<string> IntentNames => _handlers.Keys;

    public bool TryGet(string? name, [NotNullWhen(true)] out IIntentHandler? handler)
    {
        handler = null;
        return !string.IsNullOrWhiteSpace(name) && _handlers.TryGetValue(name.Trim(), out handler);
    }
}

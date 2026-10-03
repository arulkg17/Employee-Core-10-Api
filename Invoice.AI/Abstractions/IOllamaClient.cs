namespace Invoice.AI.Abstractions;

public interface IOllamaClient
{
    /// <summary>
    /// Sends a system + user message to Ollama and forces the reply to follow the given JSON schema.
    /// Returns the raw JSON text produced by the model.
    /// </summary>
    Task<string> ChatJsonAsync(
        string systemPrompt,
        string userMessage,
        object jsonSchema,
        CancellationToken ct = default);
}

using Asp.Versioning;
using Invoice.AI.Abstractions;
using Invoice.AI.Intents;
using Invoice.AI.Models;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.Coreapi.Controllers;

// NOTE: route is "api/invoice-ai" so it does NOT clash with your old api/ai/ask and api/ai/ask-multi.
// Change the namespace above if your API project uses a different root namespace.
[ApiController]
[Route("api/invoice-ai")]
[ApiVersion("1.0")]
//[Authorize]
public class InvoiceAIController : ControllerBase
{
    private readonly IAIOrchestrator _orchestrator;
    private readonly IntentCatalog _catalog;
    private readonly ILogger<InvoiceAIController> _logger;

    public InvoiceAIController(
        IAIOrchestrator orchestrator,
        IntentCatalog catalog,
        ILogger<InvoiceAIController> logger)
    {
        _orchestrator = orchestrator;
        _catalog = catalog;
        _logger = logger;
    }

    /// <summary>SINGLE intent: one business question per request.</summary>
    [HttpPost("ask")]
    public Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
        => HandleAsync(request, AskMode.Single, ct);

    /// <summary>MULTIPLE intents: one or more business questions in the same sentence.</summary>
    [HttpPost("ask-multi")]
    public Task<IActionResult> AskMulti([FromBody] AskRequest request, CancellationToken ct)
        => HandleAsync(request, AskMode.Multi, ct);

    /// <summary>Lists what the AI currently understands (handy for the UI and for students).</summary>
    [HttpGet("intents")]
    public IActionResult GetSupportedIntents()
    {
        var intents = _catalog.Handlers
            .Select(h => new { intent = h.IntentName, description = h.Description })
            .OrderBy(x => x.intent);

        return Ok(new { success = true, data = intents });
    }

    private async Task<IActionResult> HandleAsync(AskRequest? request, AskMode mode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Question))
        {
            return BadRequest(new { success = false, message = "Question is required" });
        }

        try
        {
            var result = await _orchestrator.AskAsync(request.Question, mode, ct);

            return Ok(new
            {
                success = true,
                message = "AI response generated successfully",
                data = result
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ollama is not reachable");
            return StatusCode(503, new
            {
                success = false,
                message = "The AI model is not reachable. Check that Ollama is running (ollama serve).",
                error = ex.Message
            });
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Ollama request timed out");
            return StatusCode(504, new
            {
                success = false,
                message = "The AI model took too long to answer. Try again, or increase InvoiceAI:Ollama:TimeoutSeconds.",
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing AI request");
            return StatusCode(500, new
            {
                success = false,
                message = "Error processing AI request",
                error = ex.Message
            });
        }
    }
}

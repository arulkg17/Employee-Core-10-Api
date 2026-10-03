using System.Text.Json;
using Invoice.AI.Abstractions;
using Invoice.AI.Configuration;
using Invoice.AI.Data;
using Invoice.AI.Intents;
using Invoice.AI.Intents.Handlers;
using Invoice.AI.Models;
using Invoice.AI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Invoice.AI.Tests;

/// <summary>
/// No Ollama and no SQL Server needed: the LLM is replaced by a fake that returns canned JSON,
/// and the database is replaced by InMemoryMasterDataQueryService.
/// </summary>
public class OrchestratorTests
{
    private sealed class FakeOllama : IOllamaClient
    {
        private readonly string _json;
        public FakeOllama(string json) => _json = json;

        public Task<string> ChatJsonAsync(string systemPrompt, string userMessage, object jsonSchema, CancellationToken ct = default)
            => Task.FromResult(_json);
    }

    private static AIOrchestrator Build(string fakeModelOutput)
    {
        var data = new InMemoryMasterDataQueryService();

        var handlers = new IIntentHandler[]
        {
            new CategoryItemCountHandler(data), new CategoryCountHandler(data), new ItemCountHandler(data),
            new CustomerCountHandler(data), new CustomerCountByCityHandler(data), new CustomerCountAllCitiesHandler(data),
            new VendorCountHandler(data), new UserCountHandler(data)
        };

        var catalog = new IntentCatalog(handlers);

        var extractor = new IntentExtractor(
            new FakeOllama(fakeModelOutput), data, catalog, new IntentPromptBuilder(),
            NullLogger<IntentExtractor>.Instance);

        return new AIOrchestrator(
            extractor, catalog,
            Options.Create(new InvoiceAIOptions { IncludeDebugInfo = false }),
            NullLogger<AIOrchestrator>.Instance);
    }

    private static string Json(params object[] intents) => JsonSerializer.Serialize(new { intents });

    [Fact]
    public async Task Single_InactiveWordIsFixedByCSharp_EvenWhenModelSaysActive()
    {
        var llm = Json(new { intent = "CategoryItemCount", phrase = "How many inactive items are in Rice",
                             categoryName = "rice", city = "", activeFilter = "active" });

        var result = await Build(llm).AskAsync("How many inactive items are in Rice?", AskMode.Single);

        Assert.Equal("There is 1 inactive item in the Rice category.", result.Answer);
    }

    [Fact]
    public async Task Single_TypoInCategory_IsMappedToDatabaseSpelling()
    {
        var llm = Json(new { intent = "CategoryItemCount", phrase = "How many items are in Vegetables",
                             categoryName = "Vegetables", city = "", activeFilter = "any" });

        var result = await Build(llm).AskAsync("How many items are in Vegetables?", AskMode.Single);

        Assert.Equal("There are 3 items in the Vegitables category.", result.Answer);
    }

    [Fact]
    public async Task Single_CustomersInCity()
    {
        var llm = Json(new { intent = "CustomerCountByCity", phrase = "How many active customers are in Chennai",
                             categoryName = "", city = "chennai", activeFilter = "active" });

        var result = await Build(llm).AskAsync("How many active customers are in Chennai?", AskMode.Single);

        Assert.Equal("There are 2 active customers in Chennai.", result.Answer);
    }

    [Fact]
    public async Task Single_CustomersByAllCities()
    {
        var llm = Json(new { intent = "CustomerCountAllCities", phrase = "customer count for every city",
                             categoryName = "", city = "", activeFilter = "any" });

        var result = await Build(llm).AskAsync("Show customer count for every city", AskMode.Single);

        Assert.Equal("Customers by city: Chennai 3, Mumbai 2, Pune 1.", result.Answer);
    }

    [Fact]
    public async Task Multi_TwoQuestions_AreBothAnswered()
    {
        var llm = Json(
            new { intent = "CustomerCount", phrase = "How many active customers", categoryName = "", city = "", activeFilter = "active" },
            new { intent = "VendorCount", phrase = "inactive vendors", categoryName = "", city = "", activeFilter = "inactive" });

        var result = await Build(llm).AskAsync("How many active customers and inactive vendors are there?", AskMode.Multi);

        Assert.Equal("There are 4 active customers. There is 1 inactive vendor.", result.Answer);
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public async Task Multi_ThreeQuestions_AcrossDifferentMasters()
    {
        var llm = Json(
            new { intent = "CategoryItemCount", phrase = "items in Oil", categoryName = "Oil", city = "", activeFilter = "any" },
            new { intent = "UserCount", phrase = "how many users", categoryName = "", city = "", activeFilter = "any" },
            new { intent = "CategoryCount", phrase = "how many categories", categoryName = "", city = "", activeFilter = "any" });

        var result = await Build(llm).AskAsync("How many items are in Oil, how many users and how many categories?", AskMode.Multi);

        Assert.Equal(
            "There are 2 items in the Oil category. There are 5 users. There are 7 categories.",
            result.Answer);
    }

    [Fact]
    public async Task Single_WhenModelReturnsTwoIntents_OnlyFirstIsAnswered_AndUserIsTold()
    {
        var llm = Json(
            new { intent = "CustomerCount", phrase = "customers", categoryName = "", city = "", activeFilter = "any" },
            new { intent = "VendorCount", phrase = "vendors", categoryName = "", city = "", activeFilter = "any" });

        var result = await Build(llm).AskAsync("How many customers and vendors?", AskMode.Single);

        Assert.Single(result.Results);
        Assert.Contains("only the first", result.Answer);
    }

    [Fact]
    public async Task UnknownIntent_ReturnsFriendlyMessage()
    {
        var llm = Json(new { intent = "Unknown", phrase = "weather", categoryName = "", city = "", activeFilter = "any" });

        var result = await Build(llm).AskAsync("What is the weather today?", AskMode.Single);

        Assert.Equal(AIOrchestrator.UnsupportedMessage, result.Answer);
    }

    [Fact]
    public async Task InvalidJsonFromModel_DoesNotCrash()
    {
        var result = await Build("this is not json").AskAsync("anything", AskMode.Multi);

        Assert.Equal(AIOrchestrator.UnsupportedMessage, result.Answer);
    }

    [Fact]
    public async Task UnknownCategory_ReturnsNotFoundMessage()
    {
        var llm = Json(new { intent = "CategoryItemCount", phrase = "items in Laptops",
                             categoryName = "Laptops", city = "", activeFilter = "any" });

        var result = await Build(llm).AskAsync("How many items are in Laptops?", AskMode.Single);

        Assert.Equal("I could not find the category 'Laptops'.", result.Answer);
    }
}

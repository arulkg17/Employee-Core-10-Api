using Invoice.AI.Services;
using Xunit;

namespace Invoice.AI.Tests;

public class ActiveFilterResolverTests
{
    [Theory]
    // LLM wrongly says "active" but the user wrote "inactive" -> C# fixes it
    [InlineData("How many inactive items are in Rice?", "How many inactive items are in Rice", "active", false)]
    [InlineData("How many active customers?", "active customers", "any", true)]
    // user never said active/inactive -> no filter even if the LLM invented one
    [InlineData("How many customers are there?", "customers", "active", null)]
    // word is in the question but not in this intent's phrase -> trust the LLM hint
    [InlineData("How many active customers and vendors?", "vendors", "active", true)]
    [InlineData("How many customers are not active?", "customers not active", "any", false)]
    public void Resolve_ReturnsExpected(string question, string phrase, string llm, bool? expected)
    {
        Assert.Equal(expected, ActiveFilterResolver.Resolve(question, phrase, llm));
    }
}

public class FuzzyMatcherTests
{
    private static readonly string[] Categories =
        { "Rice", "Vegitables", "Snacks", "Oil", "Soap", "General", "Pulses" };

    [Theory]
    [InlineData("Vegetables", "Vegitables")]
    [InlineData("snack", "Snacks")]
    [InlineData("rice", "Rice")]
    [InlineData("Laptops", null)]
    [InlineData("", null)]
    public void BestMatch_ReturnsExpected(string input, string? expected)
    {
        Assert.Equal(expected, FuzzyMatcher.BestMatch(input, Categories));
    }
}

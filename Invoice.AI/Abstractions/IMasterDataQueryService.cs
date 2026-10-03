using Invoice.AI.Models;

namespace Invoice.AI.Abstractions;

/// <summary>
/// Read-only questions the AI is allowed to ask about the masters
/// (Category, ItemMaster, Customer, Vendor, User).
/// The LLM never touches the database - only this interface does.
/// </summary>
public interface IMasterDataQueryService
{
    // Lookup lists (given to the LLM so it can fix typos like "Vegetables" -> "Vegitables")
    Task<IReadOnlyList<string>> GetCategoryNamesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetCityNamesAsync(CancellationToken ct = default);

    // isActive: true = active only, false = inactive only, null = all
    Task<int> CountCategoriesAsync(bool? isActive, CancellationToken ct = default);

    /// <summary>Returns null when the category does not exist.</summary>
    Task<int?> CountItemsInCategoryAsync(string categoryName, bool? itemIsActive, CancellationToken ct = default);

    Task<int> CountItemsAsync(bool? isActive, CancellationToken ct = default);
    Task<int> CountCustomersAsync(bool? isActive, CancellationToken ct = default);
    Task<int> CountCustomersByCityAsync(string city, bool? isActive, CancellationToken ct = default);
    Task<IReadOnlyList<CityCount>> CountCustomersGroupedByCityAsync(bool? isActive, CancellationToken ct = default);
    Task<int> CountVendorsAsync(bool? isActive, CancellationToken ct = default);
    Task<int> CountUsersAsync(bool? isActive, CancellationToken ct = default);
}

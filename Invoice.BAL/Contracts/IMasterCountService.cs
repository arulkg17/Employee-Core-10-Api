using Invoice.DTOs;

namespace Invoice.BAL.Contracts;

/// <summary>
/// Read-only "how many" questions about the masters. ONE place for the counting rules
/// (deleted rows never count; isActive: true = active only, false = inactive only, null = all).
/// Used by Invoice.AI and by the MasterCount API endpoints (which the MCP server calls).
/// </summary>
public interface IMasterCountService
{
    Task<IReadOnlyList<string>> GetCategoryNamesAsync();
    Task<IReadOnlyList<string>> GetCityNamesAsync();

    Task<int> CountCategoriesAsync(bool? isActive);

    /// <summary>Returns null when the category name does not exist.</summary>
    Task<int?> CountItemsInCategoryAsync(string categoryName, bool? isActive);

    Task<int> CountItemsAsync(int? categoryId, bool? isActive);
    Task<int> CountCustomersAsync(bool? isActive);
    Task<int> CountCustomersByCityAsync(string city, bool? isActive);
    Task<IReadOnlyList<CityCountDto>> CountCustomersGroupedByCityAsync(bool? isActive);
    Task<int> CountVendorsAsync(bool? isActive);
    Task<int> CountUsersAsync(bool? isActive);
}

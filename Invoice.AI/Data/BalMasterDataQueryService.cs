using Invoice.AI.Abstractions;
using Invoice.AI.Models;
using Invoice.BAL.Contracts;

namespace Invoice.AI.Data;

/// <summary>
/// Invoice.AI's data source: the BAL (IMasterCountService), never the database.
/// Handlers, orchestrator and the LLM step are unchanged - this adapter only translates.
/// Layering rule for this project: it may use Invoice.BAL.Contracts and Invoice.DTOs only
/// (no DAL, no Data, no Dapper, no IDbConnection).
/// </summary>
public sealed class BalMasterDataQueryService : IMasterDataQueryService
{
    private readonly IMasterCountService _counts;

    public BalMasterDataQueryService(IMasterCountService counts) => _counts = counts;

    public Task<IReadOnlyList<string>> GetCategoryNamesAsync(CancellationToken ct = default)
        => _counts.GetCategoryNamesAsync();

    public Task<IReadOnlyList<string>> GetCityNamesAsync(CancellationToken ct = default)
        => _counts.GetCityNamesAsync();

    public Task<int> CountCategoriesAsync(bool? isActive, CancellationToken ct = default)
        => _counts.CountCategoriesAsync(isActive);

    public Task<int?> CountItemsInCategoryAsync(string categoryName, bool? itemIsActive, CancellationToken ct = default)
        => _counts.CountItemsInCategoryAsync(categoryName, itemIsActive);

    public Task<int> CountItemsAsync(bool? isActive, CancellationToken ct = default)
        => _counts.CountItemsAsync(null, isActive);

    public Task<int> CountCustomersAsync(bool? isActive, CancellationToken ct = default)
        => _counts.CountCustomersAsync(isActive);

    public Task<int> CountCustomersByCityAsync(string city, bool? isActive, CancellationToken ct = default)
        => _counts.CountCustomersByCityAsync(city, isActive);

    public async Task<IReadOnlyList<CityCount>> CountCustomersGroupedByCityAsync(bool? isActive, CancellationToken ct = default)
        => (await _counts.CountCustomersGroupedByCityAsync(isActive))
            .Select(r => new CityCount(r.City, r.Count))
            .ToList();

    public Task<int> CountVendorsAsync(bool? isActive, CancellationToken ct = default)
        => _counts.CountVendorsAsync(isActive);

    public Task<int> CountUsersAsync(bool? isActive, CancellationToken ct = default)
        => _counts.CountUsersAsync(isActive);
}

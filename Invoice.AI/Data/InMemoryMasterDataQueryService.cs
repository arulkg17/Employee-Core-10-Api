using Invoice.AI.Abstractions;
using Invoice.AI.Models;

namespace Invoice.AI.Data;

/// <summary>
/// Fake data for the first demo and for unit tests - no SQL Server needed.
/// Turn on with  "InvoiceAI": { "UseInMemoryData": true }.
///
/// Expected numbers (handy for checking the AI):
///   Categories 7 (Soap inactive) | Items 14 (11 active, 3 inactive)
///   Customers 6 (4 active, 2 inactive) - Chennai 3, Mumbai 2, Pune 1
///   Vendors 4 (3 active, 1 inactive)   | Users 5 (4 active, 1 inactive)
/// </summary>
public sealed class InMemoryMasterDataQueryService : IMasterDataQueryService
{
    private sealed record CategoryRow(string Name, bool IsActive);
    private sealed record ItemRow(string Name, string Category, bool IsActive);
    private sealed record CustomerRow(string Name, string City, bool IsActive);
    private sealed record PartyRow(string Name, bool IsActive);

    private static readonly CategoryRow[] Categories =
    {
        new("Rice", true), new("Vegitables", true), new("Snacks", true), new("Oil", true),
        new("Soap", false), new("General", true), new("Pulses", true)
    };

    private static readonly ItemRow[] Items =
    {
        new("Basmati Rice", "Rice", true), new("Sona Masoori", "Rice", true), new("Brown Rice", "Rice", false),
        new("Tomato", "Vegitables", true), new("Potato", "Vegitables", true), new("Onion", "Vegitables", false),
        new("Chips", "Snacks", true), new("Biscuits", "Snacks", true),
        new("Sunflower Oil", "Oil", true), new("Groundnut Oil", "Oil", false),
        new("Bath Soap", "Soap", true),
        new("Toor Dal", "Pulses", true), new("Moong Dal", "Pulses", true),
        new("Salt", "General", true)
    };

    private static readonly CustomerRow[] Customers =
    {
        new("Asha", "Chennai", true), new("Ravi", "Chennai", true), new("Suresh", "Chennai", false),
        new("Meena", "Mumbai", false), new("Divya", "Mumbai", true),
        new("Karthik", "Pune", true)
    };

    private static readonly PartyRow[] Vendors =
    {
        new("Vendor A", true), new("Vendor B", true), new("Vendor C", true), new("Vendor D", false)
    };

    private static readonly PartyRow[] Users =
    {
        new("admin", true), new("teacher", true), new("student1", true), new("student2", true), new("guest", false)
    };

    private static bool Match(bool? filter, bool value) => filter is null || filter.Value == value;

    public Task<IReadOnlyList<string>> GetCategoryNamesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(Categories.Select(c => c.Name).ToList());

    public Task<IReadOnlyList<string>> GetCityNamesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(
            Customers.Select(c => c.City).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList());

    public Task<int> CountCategoriesAsync(bool? isActive, CancellationToken ct = default)
        => Task.FromResult(Categories.Count(c => Match(isActive, c.IsActive)));

    public Task<int?> CountItemsInCategoryAsync(string categoryName, bool? itemIsActive, CancellationToken ct = default)
    {
        var category = Categories.FirstOrDefault(
            c => c.Name.Equals(categoryName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (category is null)
            return Task.FromResult<int?>(null);

        var count = Items.Count(i =>
            i.Category.Equals(category.Name, StringComparison.OrdinalIgnoreCase) &&
            Match(itemIsActive, i.IsActive));

        return Task.FromResult<int?>(count);
    }

    public Task<int> CountItemsAsync(bool? isActive, CancellationToken ct = default)
        => Task.FromResult(Items.Count(i => Match(isActive, i.IsActive)));

    public Task<int> CountCustomersAsync(bool? isActive, CancellationToken ct = default)
        => Task.FromResult(Customers.Count(c => Match(isActive, c.IsActive)));

    public Task<int> CountCustomersByCityAsync(string city, bool? isActive, CancellationToken ct = default)
        => Task.FromResult(Customers.Count(c =>
            c.City.Equals(city.Trim(), StringComparison.OrdinalIgnoreCase) && Match(isActive, c.IsActive)));

    public Task<IReadOnlyList<CityCount>> CountCustomersGroupedByCityAsync(bool? isActive, CancellationToken ct = default)
    {
        var rows = Customers
            .Where(c => Match(isActive, c.IsActive))
            .GroupBy(c => c.City, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(g => new CityCount(g.Key, g.Count()))
            .ToList();

        return Task.FromResult<IReadOnlyList<CityCount>>(rows);
    }

    public Task<int> CountVendorsAsync(bool? isActive, CancellationToken ct = default)
        => Task.FromResult(Vendors.Count(v => Match(isActive, v.IsActive)));

    public Task<int> CountUsersAsync(bool? isActive, CancellationToken ct = default)
        => Task.FromResult(Users.Count(u => Match(isActive, u.IsActive)));
}

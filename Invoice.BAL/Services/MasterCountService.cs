using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Services;

/// <summary>
/// Built only on existing DAL repositories and BAL services, so there is no new SQL.
/// Where a DAL count exists (customers, items) it is used; the small masters
/// (categories, vendors, users, city grouping) are counted in memory.
/// If a master grows large, add a COUNT stored procedure + repository method and swap it in here -
/// nothing above this class changes.
/// </summary>
public class MasterCountService : IMasterCountService
{
    private readonly ICustomerRepository _customers;
    private readonly IItemmasterRepository _items;
    private readonly IUserRepository _users;
    private readonly ICategoryService _categories;
    private readonly IVendorService _vendors;

    public MasterCountService(
        ICustomerRepository customers,
        IItemmasterRepository items,
        IUserRepository users,
        ICategoryService categories,
        IVendorService vendors)
    {
        _customers = customers;
        _items = items;
        _users = users;
        _categories = categories;
        _vendors = vendors;
    }

    // ---------------- lookups ----------------

    public async Task<IReadOnlyList<string>> GetCategoryNamesAsync()
        => (await _categories.GetAllAsync())
            .Select(c => c.Name?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();

    public async Task<IReadOnlyList<string>> GetCityNamesAsync()
        => (await LiveCustomersAsync(null))
            .Select(c => (c.City ?? string.Empty).Trim())
            .Where(city => city.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(city => city)
            .ToList();

    // ---------------- counts ----------------

    public async Task<int> CountCategoriesAsync(bool? isActive)
        => (await _categories.GetAllAsync()).Count(c => isActive == null || c.IsActive == isActive);

    public async Task<int?> CountItemsInCategoryAsync(string categoryName, bool? isActive)
    {
        var wanted = categoryName.Trim();

        var category = (await _categories.GetAllAsync())
            .FirstOrDefault(c => string.Equals(c.Name?.Trim(), wanted, StringComparison.OrdinalIgnoreCase));

        if (category is null)
            return null;

        return await CountItemsAsync(category.Id, isActive);
    }

    public async Task<int> CountItemsAsync(int? categoryId, bool? isActive)
    {
        // Same stored procedure the Itemmaster API uses; we only need TotalRecords.
        var result = await _items.GetAllPagedAsync(new ItemmasterFilterDto
        {
            CategoryId = categoryId,
            IsActive = isActive,
            PageNumber = 1,
            PageSize = 1
        });

        return result.TotalRecords;
    }

    public Task<int> CountCustomersAsync(bool? isActive)
        => _customers.GetCustomerCountAsync(isActive);   // existing DAL count (excludes deleted)

    public async Task<int> CountCustomersByCityAsync(string city, bool? isActive)
    {
        var wanted = city.Trim();

        return (await LiveCustomersAsync(isActive))
            .Count(c => string.Equals((c.City ?? string.Empty).Trim(), wanted, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<CityCountDto>> CountCustomersGroupedByCityAsync(bool? isActive)
        => (await LiveCustomersAsync(isActive))
            .Select(c => (c.City ?? string.Empty).Trim())
            .Where(city => city.Length > 0)
            .GroupBy(city => city, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(g => new CityCountDto(g.Key, g.Count()))
            .ToList();

    public async Task<int> CountVendorsAsync(bool? isActive)
        => (await _vendors.GetAllAsync()).Count(v => isActive == null || v.IsActive == isActive);

    public async Task<int> CountUsersAsync(bool? isActive)
        => (await _users.GetAllAsync()).Count(u => !u.IsDeleted && (isActive == null || u.IsActive == isActive));

    // ---------------- helpers ----------------

    private async Task<List<CustomerEntity>> LiveCustomersAsync(bool? isActive)
        => (await _customers.GetAllAsync())
            .Where(c => c.IsDeleted != true && (isActive == null || c.IsActive == isActive))
            .ToList();
}

/* ------------------------------------------------------------------
   Register next to your other BAL services (Program.cs or your BAL extension):

       services.AddScoped<IMasterCountService, MasterCountService>();
------------------------------------------------------------------ */

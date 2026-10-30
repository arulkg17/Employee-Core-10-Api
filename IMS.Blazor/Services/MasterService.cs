using IMS.Blazor.Models;

namespace IMS.Blazor.Services;

/// <summary>
/// The calls every master has:  GetAllPaged | GetAll | GetById | Create | Update | Delete
/// on  api/v1/&lt;resource&gt;/...  Each master is a tiny class that only says its resource name.
/// </summary>
public abstract class MasterService<T> where T : class
{
    private readonly string _basePath;

    protected MasterService(ApiClient api, string resource)
    {
        Api = api;
        _basePath = $"v1/{resource}";
    }

    protected ApiClient Api { get; }

    protected string BasePath => _basePath;

    public async Task<PagedResult<T>> GetPagedAsync(Dictionary<string, string?> filter, int page, int pageSize)
    {
        var query = new Dictionary<string, string?>(filter)
        {
            ["pageNumber"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };

        var result = await Api.GetAsync($"{_basePath}/GetAllPaged", query);

        if (!result.Success)
        {
            throw new ApiException(result.MessageOr("Unable to load the list."));
        }

        return result.AsPaged<T>();
    }

    public async Task<List<T>> GetAllAsync()
    {
        var result = await Api.GetAsync($"{_basePath}/GetAll");

        if (!result.Success)
        {
            throw new ApiException(result.MessageOr("Unable to load the list."));
        }

        return result.AsList<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        var result = await Api.GetAsync($"{_basePath}/GetById/{id}");

        if (!result.Success)
        {
            throw new ApiException(result.MessageOr("Not found."));
        }

        return result.As<T>();
    }

    // Writes return ApiResult: the screen checks Success and shows Message.
    public Task<ApiResult> CreateAsync(object body) => Api.PostAsync($"{_basePath}/Create", body);

    public Task<ApiResult> UpdateAsync(int id, object body) => Api.PutAsync($"{_basePath}/Update/{id}", body);

    public Task<ApiResult> DeleteAsync(int id) => Api.DeleteAsync($"{_basePath}/Delete/{id}");
}

public class CategoryService : MasterService<Category>
{
    public CategoryService(ApiClient api) : base(api, "Category") { }
}

public class CustomerService : MasterService<Customer>
{
    public CustomerService(ApiClient api) : base(api, "Customer") { }
}

public class VendorService : MasterService<Vendor>
{
    public VendorService(ApiClient api) : base(api, "Vendor") { }
}

public class AppUserService : MasterService<AppUser>
{
    public AppUserService(ApiClient api) : base(api, "User") { }
}

public class ItemmasterService : MasterService<Itemmaster>
{
    public ItemmasterService(ApiClient api) : base(api, "Itemmaster") { }
}

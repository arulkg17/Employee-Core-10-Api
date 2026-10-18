using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Invoice.BAL.Contracts;
using Moq;

namespace Invoice.BAL.Test.Services;

public class MasterCountServiceTests
{
    public MasterCountServiceTests()
    {
        _customersMock = new Mock<ICustomerRepository>();
        _itemsMock = new Mock<IItemmasterRepository>();
        _usersMock = new Mock<IUserRepository>();
        _categoriesMock = new Mock<ICategoryService>();
        _vendorsMock = new Mock<IVendorService>();

        _service = new MasterCountService(
            _customersMock.Object,
            _itemsMock.Object,
            _usersMock.Object,
            _categoriesMock.Object,
            _vendorsMock.Object);
    }

    private readonly Mock<ICustomerRepository> _customersMock;
    private readonly Mock<IItemmasterRepository> _itemsMock;
    private readonly Mock<IUserRepository> _usersMock;
    private readonly Mock<ICategoryService> _categoriesMock;
    private readonly Mock<IVendorService> _vendorsMock;
    private readonly MasterCountService _service;

    [Fact]
    public async Task GetCategoryNamesAsync_ShouldReturnDistinctTrimmedOrdered()
    {
        _categoriesMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CategoryDto>
            {
                new CategoryDto { Name = " Rice " },
                new CategoryDto { Name = "rice" },
                new CategoryDto { Name = null },
                new CategoryDto { Name = "  " },
                new CategoryDto { Name = "Wheat" }
            });

        var result = await _service.GetCategoryNamesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Rice", result[0]);
        Assert.Equal("Wheat", result[1]);
    }

    [Fact]
    public async Task CountCategoriesAsync_ShouldRespectIsActiveFilter()
    {
        _categoriesMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CategoryDto>
            {
                new CategoryDto { Id = 1, IsActive = true },
                new CategoryDto { Id = 2, IsActive = false },
                new CategoryDto { Id = 3, IsActive = true }
            });

        var all = await _service.CountCategoriesAsync(null);
        var active = await _service.CountCategoriesAsync(true);
        var inactive = await _service.CountCategoriesAsync(false);

        Assert.Equal(3, all);
        Assert.Equal(2, active);
        Assert.Equal(1, inactive);
    }

    [Fact]
    public async Task CountItemsInCategoryAsync_ShouldReturnNullWhenNotFound()
    {
        _categoriesMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CategoryDto>());

        var result = await _service.CountItemsInCategoryAsync("NonExistent", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task CountItemsInCategoryAsync_ShouldReturnCountWhenFound()
    {
        _categoriesMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CategoryDto>
            {
                new CategoryDto { Id = 7, Name = "Veg" }
            });

        _itemsMock
            .Setup(x => x.GetAllPagedAsync(It.IsAny<ItemmasterFilterDto>()))
            .ReturnsAsync(new PagedResultDto<ItemmasterEntity> { TotalRecords = 42 });

        var result = await _service.CountItemsInCategoryAsync("veg", null);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task CountCustomersAsync_UsesRepositoryCount()
    {
        _customersMock
            .Setup(x => x.GetCustomerCountAsync(true))
            .ReturnsAsync(5);

        var result = await _service.CountCustomersAsync(true);

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task CountCustomersByCityAsync_And_GroupedByCity_ShouldWork()
    {
        var customers = new List<CustomerEntity>
        {
            new CustomerEntity { City = "CityA", IsDeleted = false, IsActive = true },
            new CustomerEntity { City = "CityA ", IsDeleted = false, IsActive = true },
            new CustomerEntity { City = "CityB", IsDeleted = false, IsActive = false },
            new CustomerEntity { City = null, IsDeleted = false, IsActive = true },
            new CustomerEntity { City = "CityA", IsDeleted = true, IsActive = true }
        };

        _customersMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(customers);

        var byCity = await _service.CountCustomersByCityAsync("CityA", null);
        var grouped = (await _service.CountCustomersGroupedByCityAsync(null)).ToList();

        Assert.Equal(2, byCity);
        Assert.Equal(2, grouped.Count);
        Assert.Equal("CityA", grouped[0].City);
        Assert.Equal(2, grouped[0].Count);
    }

    [Fact]
    public async Task CountVendorsAsync_ShouldRespectIsActive()
    {
        _vendorsMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<VendorDto>
            {
                new VendorDto { Id = 1, IsActive = true },
                new VendorDto { Id = 2, IsActive = false }
            });

        var all = await _service.CountVendorsAsync(null);
        var active = await _service.CountVendorsAsync(true);

        Assert.Equal(2, all);
        Assert.Equal(1, active);
    }

    [Fact]
    public async Task CountUsersAsync_ShouldExcludeDeletedAndRespectIsActive()
    {
        _usersMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<UserEntity>
            {
                new UserEntity { Id = 1, IsDeleted = false, IsActive = true },
                new UserEntity { Id = 2, IsDeleted = false, IsActive = false },
                new UserEntity { Id = 3, IsDeleted = true, IsActive = true }
            });

        var all = await _service.CountUsersAsync(null);
        var active = await _service.CountUsersAsync(true);

        Assert.Equal(2, all);
        Assert.Equal(1, active);
    }
}

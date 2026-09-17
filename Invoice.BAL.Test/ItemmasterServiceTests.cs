using AutoMapper;
using Invoice.BAL.Mapper;
using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Moq;
using Microsoft.Extensions.Logging;

namespace Invoice.BAL.Test.Services;

public class ItemmasterServiceTests
{
    public ItemmasterServiceTests()
    {
        _repositoryMock = new Mock<IItemmasterRepository>();

        using var loggerFactory =
            LoggerFactory.Create(builder => { });

        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile<ItemmasterProfile>();
            },
            loggerFactory);

        _mapper = configuration.CreateMapper();

        _service = new ItemmasterServiceEFSp(
            _repositoryMock.Object,
            _mapper);
    }
    private readonly Mock<IItemmasterRepository> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly ItemmasterServiceEFSp _service;


    // ---------------------------------------------------------
    // Test helper
    // ---------------------------------------------------------
    private static ItemmasterEntity CreateItem(
        int id,
        int categoryId = 1,
        bool? isActive = true)
    {
        return new ItemmasterEntity
        {
            Id = id,
            CategoryId = categoryId,
            ItemBarCode = $"BAR{id:000}",
            ItemCode = $"ITM{id:000}",
            ItemName = $"Test Item {id}",
            Uom = "PCS",
            Rate = 100.00m,
            MinimumStock = 10.00m,
            MaximumStock = 100.00m,
            IsActive = isActive,
            CreatedBy = "TEST",
            CreatedDate = DateTime.UtcNow
        };
    }

    private static ItemmasterDto CreateItemDto(
        int id = 0,
        int categoryId = 1,
        bool? isActive = true)
    {
        return new ItemmasterDto
        {
            Id = id,
            CategoryId = categoryId,
            ItemBarCode = $"BAR{id:000}",
            ItemCode = $"ITM{id:000}",
            ItemName = $"Test Item {id}",
            Uom = "PCS",
            Rate = 100.00m,
            MinimumStock = 10.00m,
            MaximumStock = 100.00m,
            IsActive = isActive,
            CreatedBy = "TEST",
            CreatedDate = DateTime.UtcNow
        };
    }

    // ---------------------------------------------------------
    // AddAsync
    // ---------------------------------------------------------
    [Fact]
    public async Task AddAsync_ShouldReturnInsertedId()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ItemmasterEntity>()))
            .ReturnsAsync(301);

        var dto = CreateItemDto();

        // Act
        var result = await _service.AddAsync(dto);

        // Assert
        Assert.Equal(301, result);

        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ItemmasterEntity>()),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GetAllAsync
    // ---------------------------------------------------------
    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedItems()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ItemmasterEntity>
            {
                CreateItem(1),
                CreateItem(2)
            });

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);

        Assert.Equal("ITM001", result[0].ItemCode);
        Assert.Equal("ITM002", result[1].ItemCode);

        Assert.Equal("Test Item 1", result[0].ItemName);
        Assert.Equal("Test Item 2", result[1].ItemName);

        Assert.Equal(1, result[0].CategoryId);
        Assert.Equal(1, result[1].CategoryId);

        Assert.Equal("PCS", result[0].Uom);
        Assert.Equal("PCS", result[1].Uom);

        _repositoryMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GetByIdAsync - Found
    // ---------------------------------------------------------
    [Fact]
    public async Task GetByIdAsync_ShouldReturnItem_WhenFound()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(CreateItem(10));

        // Act
        var result = await _service.GetByIdAsync(10);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(10, result!.Id);
        Assert.Equal("ITM010", result.ItemCode);
        Assert.Equal("Test Item 10", result.ItemName);

        _repositoryMock.Verify(
            x => x.GetByIdAsync(10),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GetByIdAsync - Not Found
    // ---------------------------------------------------------
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((ItemmasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);

        _repositoryMock.Verify(
            x => x.GetByIdAsync(999),
            Times.Once);
    }

    // ---------------------------------------------------------
    // UpdateAsync
    // ---------------------------------------------------------
    [Fact]
    public async Task UpdateAsync_ShouldReturnRepositoryResult()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ItemmasterEntity>()))
            .ReturnsAsync(true);

        var dto = CreateItemDto(
            id: 10,
            categoryId: 5,
            isActive: true);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        Assert.True(result);

        _repositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ItemmasterEntity>()),
            Times.Once);
    }

    // ---------------------------------------------------------
    // DeleteAsync
    // ---------------------------------------------------------
    [Fact]
    public async Task DeleteAsync_ShouldReturnRepositoryResult()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.DeleteAsync(10))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(10);

        // Assert
        Assert.True(result);

        _repositoryMock.Verify(
            x => x.DeleteAsync(10),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GetAllPagedAsync
    // ---------------------------------------------------------
    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnPagedItems()
    {
        // Arrange
        var filter = new ItemmasterFilterDto();

        _repositoryMock
            .Setup(x => x.GetAllPagedAsync(It.IsAny<ItemmasterFilterDto>()))
            .ReturnsAsync(new PagedResultDto<ItemmasterEntity>
            {
                Data = new List<ItemmasterEntity>
                {
                    CreateItem(1),
                    CreateItem(2)
                },
                TotalRecords = 20
            });

        // Act
        var result = await _service.GetAllPagedAsync(filter);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(20, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());

        var items = result.Data.ToList();

        Assert.Equal(1, items[0].Id);
        Assert.Equal(2, items[1].Id);

        Assert.Equal("ITM001", items[0].ItemCode);
        Assert.Equal("ITM002", items[1].ItemCode);

        _repositoryMock.Verify(
            x => x.GetAllPagedAsync(filter),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GetActiveItemCountByCategoryAsync
    // ---------------------------------------------------------
    [Fact]
    public async Task GetActiveItemCountByCategoryAsync_ShouldCountOnlyActiveItems()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ItemmasterEntity>
            {
                CreateItem(1, categoryId: 10, isActive: true),
                CreateItem(2, categoryId: 10, isActive: true),
                CreateItem(3, categoryId: 10, isActive: false),
                CreateItem(4, categoryId: 20, isActive: true)
            });

        // Act
        var result =
            await _service.GetActiveItemCountByCategoryAsync(10);

        // Assert
        Assert.Equal(2, result);

        _repositoryMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GetActiveItemCountByCategoryAsync - Zero
    // ---------------------------------------------------------
    [Fact]
    public async Task GetActiveItemCountByCategoryAsync_ShouldReturnZero_WhenNoActiveItems()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ItemmasterEntity>
            {
                CreateItem(1, categoryId: 10, isActive: false),
                CreateItem(2, categoryId: 20, isActive: true)
            });

        // Act
        var result =
            await _service.GetActiveItemCountByCategoryAsync(10);

        // Assert
        Assert.Equal(0, result);

        _repositoryMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }
}


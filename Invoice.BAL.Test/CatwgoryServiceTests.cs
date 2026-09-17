using AutoMapper;
using Invoice.BAL.Mapper;
using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Moq;
using Microsoft.Extensions.Logging;

namespace Invoice.BAL.Test.Services;

public class CategoryServiceTests
{
    public CategoryServiceTests()
    {
        _repositoryMock = new Mock<ICategoryRepository>();

        using var loggerFactory =
            LoggerFactory.Create(builder => { });

        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile<CategoryProfile>();
            },
            loggerFactory);

        _mapper = configuration.CreateMapper();

        _service = new CategoryService(
            _repositoryMock.Object,
            _mapper);
    }
    private readonly Mock<ICategoryRepository> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly CategoryService _service;

   
    [Fact]
    public async Task AddAsync_ShouldMapDtoAndReturnRepositoryId()
    {
        var dto = new CategoryDto();

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<CategoryEntity>()))
            .ReturnsAsync(101);

        var result = await _service.AddAsync(dto);

        Assert.Equal(101, result);

        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CategoryEntity>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedCategories()
    {
        var entities = new List<CategoryEntity>
        {
            new CategoryEntity { Id = 1 },
            new CategoryEntity { Id = 2 }
        };

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(entities);

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);

        _repositoryMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMappedCategory_WhenFound()
    {
        var entity = new CategoryEntity
        {
            Id = 10
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(entity);

        var result = await _service.GetByIdAsync(10);

        Assert.NotNull(result);
        Assert.Equal(10, result!.Id);

        _repositoryMock.Verify(
            x => x.GetByIdAsync(10),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((CategoryEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);

        _repositoryMock.Verify(
            x => x.GetByIdAsync(999),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldMapDtoAndReturnRepositoryResult()
    {
        var dto = new CategoryDto();

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<CategoryEntity>()))
            .ReturnsAsync(true);

        var result = await _service.UpdateAsync(dto);

        Assert.True(result);

        _repositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<CategoryEntity>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnRepositoryResult()
    {
        _repositoryMock
            .Setup(x => x.DeleteAsync(10))
            .ReturnsAsync(true);

        var result = await _service.DeleteAsync(10);

        Assert.True(result);

        _repositoryMock.Verify(
            x => x.DeleteAsync(10),
            Times.Once);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnMappedPagedResult()
    {
        var repositoryResult = new PagedResultDto<CategoryEntity>
        {
            Data = new List<CategoryEntity>
            {
                new CategoryEntity { Id = 1 },
                new CategoryEntity { Id = 2 }
            },
            TotalRecords = 5
        };

        _repositoryMock
            .Setup(x => x.GetAllPagedAsync(
                "CAT",
                "Rice",
                1,
                2))
            .ReturnsAsync(repositoryResult);

        var result = await _service.GetAllPagedAsync(
            "CAT",
            "Rice",
            1,
            2);

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());

        _repositoryMock.Verify(
            x => x.GetAllPagedAsync(
                "CAT",
                "Rice",
                1,
                2),
            Times.Once);
    }
}


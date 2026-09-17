using AutoMapper;
using Invoice.BAL.Mapper;
using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.Extensions.Logging;
using Moq;

namespace Invoice.BAL.Test.Services;

public class VendorServiceTests
{
    private readonly Mock<IVendorRepository> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly VendorServiceEFSp _service;

    public VendorServiceTests()
    {
        _repositoryMock = new Mock<IVendorRepository>();

        using var loggerFactory =
            LoggerFactory.Create(builder => { });

        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile<VendorProfile>();
            },
            loggerFactory);

        _mapper = configuration.CreateMapper();

        _service = new VendorServiceEFSp(
            _repositoryMock.Object,
            _mapper);
    }

    // ---------------------------------------------------------
    // AddAsync
    // ---------------------------------------------------------
    [Fact]
    public async Task AddAsync_ShouldReturnInsertedId()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<VendorEntity>()))
            .ReturnsAsync(201);

        var dto = new VendorDto();

        // Act
        var result = await _service.AddAsync(dto);

        // Assert
        Assert.Equal(201, result);

        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<VendorEntity>()),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GetAllAsync
    // ---------------------------------------------------------
    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedVendors()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<VendorEntity>
            {
                new VendorEntity { Id = 1 },
                new VendorEntity { Id = 2 }
            });

        // Act
        var result =
            (await _service.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);

        _repositoryMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GetByIdAsync - Found
    // ---------------------------------------------------------
    [Fact]
    public async Task GetByIdAsync_ShouldReturnVendor_WhenFound()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(
                new VendorEntity { Id = 10 });

        // Act
        var result =
            await _service.GetByIdAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result!.Id);

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
            .ReturnsAsync(
                (VendorEntity?)null);

        // Act
        var result =
            await _service.GetByIdAsync(999);

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
            .Setup(x => x.UpdateAsync(
                It.IsAny<VendorEntity>()))
            .ReturnsAsync(true);

        var dto = new VendorDto();

        // Act
        var result =
            await _service.UpdateAsync(dto);

        // Assert
        Assert.True(result);

        _repositoryMock.Verify(
            x => x.UpdateAsync(
                It.IsAny<VendorEntity>()),
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
        var result =
            await _service.DeleteAsync(10);

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
    public async Task GetAllPagedAsync_ShouldReturnPagedVendors()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetAllPagedAsync(
                "VEN",
                "ABC",
                "9999999999",
                "Chennai",
                1,
                10))
            .ReturnsAsync(
                new PagedResultDto<VendorEntity>
                {
                    Data = new List<VendorEntity>
                    {
                        new VendorEntity { Id = 1 },
                        new VendorEntity { Id = 2 }
                    },
                    TotalRecords = 15
                });

        // Act
        var result =
            await _service.GetAllPagedAsync(
                "VEN",
                "ABC",
                "9999999999",
                "Chennai",
                1,
                10);

        // Assert
        Assert.Equal(15, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());

        _repositoryMock.Verify(
            x => x.GetAllPagedAsync(
                "VEN",
                "ABC",
                "9999999999",
                "Chennai",
                1,
                10),
            Times.Once);
    }
}


using AutoMapper;
using Invoice.BAL.Mapper;
using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.Extensions.Logging;
using Moq;

namespace Invoice.BAL.Test.Services;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _repositoryMock = new Mock<ICustomerRepository>();

        using var loggerFactory =
            LoggerFactory.Create(builder => { });

        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile<CustomerProfile>();
            },
            loggerFactory);

        _mapper = configuration.CreateMapper();

        _service = new CustomerService(
            _repositoryMock.Object,
            _mapper);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnInsertedId()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<CustomerEntity>()))
            .ReturnsAsync(101);

        // Act
        var result = await _service.AddAsync(new CustomerDto());

        // Assert
        Assert.Equal(101, result);

        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CustomerEntity>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedCustomers()
    {
        // Arrange
        var entities = new List<CustomerEntity>
        {
            new CustomerEntity { Id = 1 },
            new CustomerEntity { Id = 2 }
        };

        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(entities);

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

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCustomer_WhenFound()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(
                new CustomerEntity { Id = 10 });

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

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync(
                (CustomerEntity?)null);

        // Act
        var result =
            await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);

        _repositoryMock.Verify(
            x => x.GetByIdAsync(999),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnRepositoryResult()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.UpdateAsync(
                It.IsAny<CustomerEntity>()))
            .ReturnsAsync(true);

        // Act
        var result =
            await _service.UpdateAsync(new CustomerDto());

        // Assert
        Assert.True(result);

        _repositoryMock.Verify(
            x => x.UpdateAsync(
                It.IsAny<CustomerEntity>()),
            Times.Once);
    }

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

    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnPagedCustomers()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetAllPagedAsync(
                "CUS",
                "John",
                "9999999999",
                "Chennai",
                1,
                10))
            .ReturnsAsync(
                new PagedResultDto<CustomerEntity>
                {
                    Data = new List<CustomerEntity>
                    {
                        new CustomerEntity { Id = 1 },
                        new CustomerEntity { Id = 2 }
                    },
                    TotalRecords = 12
                });

        // Act
        var result =
            await _service.GetAllPagedAsync(
                "CUS",
                "John",
                "9999999999",
                "Chennai",
                1,
                10);

        // Assert
        Assert.Equal(12, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());

        _repositoryMock.Verify(
            x => x.GetAllPagedAsync(
                "CUS",
                "John",
                "9999999999",
                "Chennai",
                1,
                10),
            Times.Once);
    }
}

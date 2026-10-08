using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Invoice.CoreAPI.Test.Controllers;

public class SalesInvoiceControllerTests
{
    private readonly Mock<ISalesInvoiceService> _serviceMock = new();
    private readonly Mock<ILogger<SalesInvoiceController>> _loggerMock = new();
    private readonly SalesInvoiceController _controller;

    public SalesInvoiceControllerTests()
    {
        _controller = new SalesInvoiceController(_serviceMock.Object, _loggerMock.Object);

        ControllerTestHelpers.SetUser(_controller, "alice");
    }

    private static ApiResponse<T> Payload<T>(ObjectResult result)
        => Assert.IsType<ApiResponse<T>>(result.Value);

    // ============================================================
    // GetAll
    // ============================================================
    [Fact]
    public async Task GetAll_ReturnsOk_WithInvoices()
    {
        _serviceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<SalesInvoiceDto>
        {
            new() { Id = 1, InvoiceNumber = "INV-1" },
            new() { Id = 2, InvoiceNumber = "INV-2" }
        });

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(2, Payload<IEnumerable<SalesInvoiceDto>>(ok).Data!.Count());
    }

    [Fact]
    public async Task GetAll_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.GetAllAsync()).ThrowsAsync(new Exception("Database error"));

        var result = await _controller.GetAll();

        var status = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, status.StatusCode);
        Assert.False(Payload<string>(status).Success);
    }

    // ============================================================
    // GetById
    // ============================================================
    [Fact]
    public async Task GetById_ReturnsOk_WhenInvoiceExists()
    {
        _serviceMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new SalesInvoiceDto { Id = 1 });

        var result = await _controller.GetById(1);

        Assert.Equal(1, Payload<SalesInvoiceDto>(Assert.IsType<OkObjectResult>(result)).Data!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenInvoiceDoesNotExist()
    {
        _serviceMock.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((SalesInvoiceDto?)null);

        Assert.IsType<NotFoundObjectResult>(await _controller.GetById(99));
    }

    [Fact]
    public async Task GetById_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.GetByIdAsync(1)).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.GetById(1)).StatusCode);
    }

    // ============================================================
    // Create
    // ============================================================
    [Fact]
    public async Task Create_ReturnsOk_WithNewId_AndSetsCreatedByFromUser()
    {
        SalesInvoiceDto? sent = null;
        _serviceMock
            .Setup(x => x.AddAsync(It.IsAny<SalesInvoiceDto>()))
            .Callback<SalesInvoiceDto>(d => sent = d)
            .ReturnsAsync(21);

        var result = await _controller.Create(new SalesInvoiceDto { CreatedBy = "someone-else" });

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(21, Payload<int>(ok).Data);
        Assert.Equal("alice", sent!.CreatedBy);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenValidationFails()
    {
        _serviceMock
            .Setup(x => x.AddAsync(It.IsAny<SalesInvoiceDto>()))
            .ThrowsAsync(new BusinessRuleException("A sales invoice must have at least one line."));

        var result = await _controller.Create(new SalesInvoiceDto());

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ApiResponse<string>>(bad.Value);

        Assert.Equal("A sales invoice must have at least one line.", body.Message);
        Assert.Equal("400", body.Error!.Code);
    }

    [Fact]
    public async Task Create_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.AddAsync(It.IsAny<SalesInvoiceDto>())).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.Create(new SalesInvoiceDto())).StatusCode);
    }

    // ============================================================
    // Update
    // ============================================================
    [Fact]
    public async Task Update_ReturnsOk_AndUsesRouteIdAndUser()
    {
        SalesInvoiceDto? sent = null;
        _serviceMock
            .Setup(x => x.UpdateAsync(It.IsAny<SalesInvoiceDto>()))
            .Callback<SalesInvoiceDto>(d => sent = d)
            .ReturnsAsync(true);

        var result = await _controller.Update(7, new SalesInvoiceDto { Id = 1 });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(7, sent!.Id);
        Assert.Equal("alice", sent.UpdatedBy);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenInvoiceDoesNotExist()
    {
        _serviceMock.Setup(x => x.UpdateAsync(It.IsAny<SalesInvoiceDto>())).ReturnsAsync(false);

        Assert.IsType<NotFoundObjectResult>(await _controller.Update(7, new SalesInvoiceDto()));
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenInvoiceIsPosted()
    {
        _serviceMock
            .Setup(x => x.UpdateAsync(It.IsAny<SalesInvoiceDto>()))
            .ThrowsAsync(new BusinessRuleException("Only Draft sales invoices can be edited."));

        Assert.IsType<BadRequestObjectResult>(await _controller.Update(7, new SalesInvoiceDto()));
    }

    [Fact]
    public async Task Update_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.UpdateAsync(It.IsAny<SalesInvoiceDto>())).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.Update(7, new SalesInvoiceDto())).StatusCode);
    }

    // ============================================================
    // Delete
    // ============================================================
    [Fact]
    public async Task Delete_ReturnsOk_WhenDeleted()
    {
        _serviceMock.Setup(x => x.DeleteAsync(3, "alice")).ReturnsAsync(true);

        Assert.IsType<OkObjectResult>(await _controller.Delete(3));
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenInvoiceDoesNotExist()
    {
        _serviceMock.Setup(x => x.DeleteAsync(3, "alice")).ReturnsAsync(false);

        Assert.IsType<NotFoundObjectResult>(await _controller.Delete(3));
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenInvoiceIsPosted()
    {
        _serviceMock
            .Setup(x => x.DeleteAsync(3, "alice"))
            .ThrowsAsync(new BusinessRuleException("Only Draft sales invoices can be deleted."));

        Assert.IsType<BadRequestObjectResult>(await _controller.Delete(3));
    }

    [Fact]
    public async Task Delete_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.DeleteAsync(3, "alice")).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.Delete(3)).StatusCode);
    }

    // ============================================================
    // GetAllPaged
    // ============================================================
    [Fact]
    public async Task GetAllPaged_ReturnsOk_AndPassesFilters()
    {
        _serviceMock
            .Setup(x => x.GetAllPagedAsync("INV", 3, "Draft", 2, 5))
            .ReturnsAsync(new PagedResultDto<SalesInvoiceDto>
            {
                Data = new List<SalesInvoiceDto> { new() { Id = 1 } },
                TotalRecords = 6
            });

        var result = await _controller.GetAllPaged("INV", 3, "Draft", 2, 5);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(6, Payload<PagedResultDto<SalesInvoiceDto>>(ok).Data!.TotalRecords);
    }

    [Fact]
    public async Task GetAllPaged_UsesDefaultPaging()
    {
        _serviceMock
            .Setup(x => x.GetAllPagedAsync(null, null, null, 1, 10))
            .ReturnsAsync(new PagedResultDto<SalesInvoiceDto>());

        await _controller.GetAllPaged(null, null, null);

        _serviceMock.Verify(x => x.GetAllPagedAsync(null, null, null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetAllPaged_Returns500_WhenServiceThrows()
    {
        _serviceMock
            .Setup(x => x.GetAllPagedAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.GetAllPaged(null, null, null)).StatusCode);
    }

    // ============================================================
    // Post
    // ============================================================
    [Fact]
    public async Task PostInvoice_ReturnsOk_AndPassesUser()
    {
        _serviceMock.Setup(x => x.PostAsync(4, "alice")).ReturnsAsync(true);

        var result = await _controller.PostInvoice(4);

        Assert.IsType<OkObjectResult>(result);
        _serviceMock.Verify(x => x.PostAsync(4, "alice"), Times.Once);
    }

    [Fact]
    public async Task PostInvoice_ReturnsBadRequest_WhenStockIsInsufficient()
    {
        _serviceMock
            .Setup(x => x.PostAsync(4, "alice"))
            .ThrowsAsync(new BusinessRuleException("Insufficient stock for item A01 (available 1, required 5)."));

        var result = await _controller.PostInvoice(4);

        var bad = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Contains("Insufficient stock", Assert.IsType<ApiResponse<string>>(bad.Value).Message);
    }

    [Fact]
    public async Task PostInvoice_ReturnsNotFound_WhenInvoiceDoesNotExist()
    {
        _serviceMock.Setup(x => x.PostAsync(4, "alice")).ThrowsAsync(new NotFoundException("Sales invoice not found."));

        Assert.IsType<NotFoundObjectResult>(await _controller.PostInvoice(4));
    }

    [Fact]
    public async Task PostInvoice_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.PostAsync(4, "alice")).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.PostInvoice(4)).StatusCode);
    }

    // ============================================================
    // Cancel
    // ============================================================
    [Fact]
    public async Task Cancel_ReturnsOk_AndPassesUser()
    {
        _serviceMock.Setup(x => x.CancelAsync(4, "alice")).ReturnsAsync(true);

        Assert.IsType<OkObjectResult>(await _controller.Cancel(4));
        _serviceMock.Verify(x => x.CancelAsync(4, "alice"), Times.Once);
    }

    [Fact]
    public async Task Cancel_ReturnsBadRequest_WhenAlreadyCancelled()
    {
        _serviceMock
            .Setup(x => x.CancelAsync(4, "alice"))
            .ThrowsAsync(new BusinessRuleException("The sales invoice is already cancelled."));

        Assert.IsType<BadRequestObjectResult>(await _controller.Cancel(4));
    }

    [Fact]
    public async Task Cancel_ReturnsNotFound_WhenInvoiceDoesNotExist()
    {
        _serviceMock.Setup(x => x.CancelAsync(4, "alice")).ThrowsAsync(new NotFoundException("Sales invoice not found."));

        Assert.IsType<NotFoundObjectResult>(await _controller.Cancel(4));
    }

    [Fact]
    public async Task Cancel_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.CancelAsync(4, "alice")).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.Cancel(4)).StatusCode);
    }
}

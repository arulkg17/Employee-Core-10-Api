using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Invoice.CoreAPI.Test.Controllers;

public class ReceiptControllerTests
{
    private readonly Mock<IReceiptService> _serviceMock = new();
    private readonly Mock<ILogger<ReceiptController>> _loggerMock = new();
    private readonly ReceiptController _controller;

    public ReceiptControllerTests()
    {
        _controller = new ReceiptController(_serviceMock.Object, _loggerMock.Object);

        ControllerTestHelpers.SetUser(_controller, "alice");
    }

    private static ApiResponse<T> Payload<T>(ObjectResult result)
        => Assert.IsType<ApiResponse<T>>(result.Value);

    // ============================================================
    // GetAll
    // ============================================================
    [Fact]
    public async Task GetAll_ReturnsOk_WithReceipts()
    {
        _serviceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ReceiptDto>
        {
            new() { Id = 1, ReceiptNumber = "GRN-1" },
            new() { Id = 2, ReceiptNumber = "GRN-2" }
        });

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Payload<IEnumerable<ReceiptDto>>(ok);

        Assert.True(body.Success);
        Assert.Equal(2, body.Data!.Count());
    }

    [Fact]
    public async Task GetAll_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.GetAllAsync()).ThrowsAsync(new Exception("Database error"));

        var result = await _controller.GetAll();

        var status = Assert.IsType<ObjectResult>(result);
        var body = Payload<string>(status);

        Assert.Equal(500, status.StatusCode);
        Assert.False(body.Success);
        Assert.Equal("Database error", body.Error!.Details);
    }

    // ============================================================
    // GetById
    // ============================================================
    [Fact]
    public async Task GetById_ReturnsOk_WhenReceiptExists()
    {
        _serviceMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new ReceiptDto { Id = 1 });

        var result = await _controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(1, Payload<ReceiptDto>(ok).Data!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenReceiptDoesNotExist()
    {
        _serviceMock.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((ReceiptDto?)null);

        var result = await _controller.GetById(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetById_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.GetByIdAsync(1)).ThrowsAsync(new Exception("boom"));

        var result = await _controller.GetById(1);

        Assert.Equal(500, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    // ============================================================
    // Create
    // ============================================================
    [Fact]
    public async Task Create_ReturnsOk_WithNewId_AndSetsCreatedByFromUser()
    {
        ReceiptDto? sent = null;
        _serviceMock
            .Setup(x => x.AddAsync(It.IsAny<ReceiptDto>()))
            .Callback<ReceiptDto>(d => sent = d)
            .ReturnsAsync(15);

        var result = await _controller.Create(new ReceiptDto { CreatedBy = "someone-else" });

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(15, Payload<int>(ok).Data);
        Assert.Equal("alice", sent!.CreatedBy);
    }

    [Fact]
    public async Task Create_UsesSystemUser_WhenNobodyIsLoggedIn()
    {
        var anonymous = new ReceiptController(_serviceMock.Object, _loggerMock.Object);

        ReceiptDto? sent = null;
        _serviceMock
            .Setup(x => x.AddAsync(It.IsAny<ReceiptDto>()))
            .Callback<ReceiptDto>(d => sent = d)
            .ReturnsAsync(1);

        await anonymous.Create(new ReceiptDto());

        Assert.Equal("system", sent!.CreatedBy);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenBusinessRuleIsViolated()
    {
        _serviceMock
            .Setup(x => x.AddAsync(It.IsAny<ReceiptDto>()))
            .ThrowsAsync(new BusinessRuleException("Goods can be received only against an Approved purchase order."));

        var result = await _controller.Create(new ReceiptDto());

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ApiResponse<string>>(bad.Value);

        Assert.False(body.Success);
        Assert.Contains("Approved", body.Message);
        Assert.Equal("400", body.Error!.Code);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenPurchaseOrderDoesNotExist()
    {
        _serviceMock
            .Setup(x => x.AddAsync(It.IsAny<ReceiptDto>()))
            .ThrowsAsync(new NotFoundException("Purchase order not found."));

        var result = await _controller.Create(new ReceiptDto());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.AddAsync(It.IsAny<ReceiptDto>())).ThrowsAsync(new Exception("boom"));

        var result = await _controller.Create(new ReceiptDto());

        Assert.Equal(500, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    // ============================================================
    // Update
    // ============================================================
    [Fact]
    public async Task Update_ReturnsOk_AndUsesRouteIdAndUser()
    {
        ReceiptDto? sent = null;
        _serviceMock
            .Setup(x => x.UpdateAsync(It.IsAny<ReceiptDto>()))
            .Callback<ReceiptDto>(d => sent = d)
            .ReturnsAsync(true);

        var result = await _controller.Update(7, new ReceiptDto { Id = 1 });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(7, sent!.Id);
        Assert.Equal("alice", sent.UpdatedBy);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenReceiptDoesNotExist()
    {
        _serviceMock.Setup(x => x.UpdateAsync(It.IsAny<ReceiptDto>())).ReturnsAsync(false);

        var result = await _controller.Update(7, new ReceiptDto());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenReceiptIsNotDraft()
    {
        _serviceMock
            .Setup(x => x.UpdateAsync(It.IsAny<ReceiptDto>()))
            .ThrowsAsync(new BusinessRuleException("Only Draft receipts can be edited."));

        var result = await _controller.Update(7, new ReceiptDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.UpdateAsync(It.IsAny<ReceiptDto>())).ThrowsAsync(new Exception("boom"));

        var result = await _controller.Update(7, new ReceiptDto());

        Assert.Equal(500, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    // ============================================================
    // Delete
    // ============================================================
    [Fact]
    public async Task Delete_ReturnsOk_WhenDeleted()
    {
        _serviceMock.Setup(x => x.DeleteAsync(3, "alice")).ReturnsAsync(true);

        var result = await _controller.Delete(3);

        Assert.IsType<OkObjectResult>(result);
        _serviceMock.Verify(x => x.DeleteAsync(3, "alice"), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenReceiptDoesNotExist()
    {
        _serviceMock.Setup(x => x.DeleteAsync(3, "alice")).ReturnsAsync(false);

        Assert.IsType<NotFoundObjectResult>(await _controller.Delete(3));
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenReceiptIsPosted()
    {
        _serviceMock
            .Setup(x => x.DeleteAsync(3, "alice"))
            .ThrowsAsync(new BusinessRuleException("Only Draft receipts can be deleted."));

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
            .Setup(x => x.GetAllPagedAsync("GRN", 10, 2, "Posted", 3, 5))
            .ReturnsAsync(new PagedResultDto<ReceiptDto>
            {
                Data = new List<ReceiptDto> { new() { Id = 1 } },
                TotalRecords = 11
            });

        var result = await _controller.GetAllPaged("GRN", 10, 2, "Posted", 3, 5);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(11, Payload<PagedResultDto<ReceiptDto>>(ok).Data!.TotalRecords);
    }

    [Fact]
    public async Task GetAllPaged_Returns500_WhenServiceThrows()
    {
        _serviceMock
            .Setup(x => x.GetAllPagedAsync(null, null, null, null, 1, 10))
            .ThrowsAsync(new Exception("boom"));

        var result = await _controller.GetAllPaged(null, null, null, null);

        Assert.Equal(500, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    // ============================================================
    // Post
    // ============================================================
    [Fact]
    public async Task PostReceipt_ReturnsOk_AndPassesUser()
    {
        _serviceMock.Setup(x => x.PostAsync(4, "alice")).ReturnsAsync(true);

        var result = await _controller.PostReceipt(4);

        Assert.IsType<OkObjectResult>(result);
        _serviceMock.Verify(x => x.PostAsync(4, "alice"), Times.Once);
    }

    [Fact]
    public async Task PostReceipt_ReturnsBadRequest_WhenReceiptCannotBePosted()
    {
        _serviceMock
            .Setup(x => x.PostAsync(4, "alice"))
            .ThrowsAsync(new BusinessRuleException("Received quantity exceeds the outstanding quantity."));

        Assert.IsType<BadRequestObjectResult>(await _controller.PostReceipt(4));
    }

    [Fact]
    public async Task PostReceipt_ReturnsNotFound_WhenReceiptDoesNotExist()
    {
        _serviceMock.Setup(x => x.PostAsync(4, "alice")).ThrowsAsync(new NotFoundException("Receipt not found."));

        Assert.IsType<NotFoundObjectResult>(await _controller.PostReceipt(4));
    }

    [Fact]
    public async Task PostReceipt_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.PostAsync(4, "alice")).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.PostReceipt(4)).StatusCode);
    }

    // ============================================================
    // Cancel
    // ============================================================
    [Fact]
    public async Task Cancel_ReturnsOk_AndPassesUser()
    {
        _serviceMock.Setup(x => x.CancelAsync(4, "alice")).ReturnsAsync(true);

        var result = await _controller.Cancel(4);

        Assert.IsType<OkObjectResult>(result);
        _serviceMock.Verify(x => x.CancelAsync(4, "alice"), Times.Once);
    }

    [Fact]
    public async Task Cancel_ReturnsBadRequest_WhenStockWasAlreadySold()
    {
        _serviceMock
            .Setup(x => x.CancelAsync(4, "alice"))
            .ThrowsAsync(new BusinessRuleException("Cannot cancel: stock of item A01 has already been sold."));

        var result = await _controller.Cancel(4);

        var bad = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Contains("already been sold", Assert.IsType<ApiResponse<string>>(bad.Value).Message);
    }

    [Fact]
    public async Task Cancel_ReturnsNotFound_WhenReceiptDoesNotExist()
    {
        _serviceMock.Setup(x => x.CancelAsync(4, "alice")).ThrowsAsync(new NotFoundException("Receipt not found."));

        Assert.IsType<NotFoundObjectResult>(await _controller.Cancel(4));
    }

    [Fact]
    public async Task Cancel_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.CancelAsync(4, "alice")).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.Cancel(4)).StatusCode);
    }
}

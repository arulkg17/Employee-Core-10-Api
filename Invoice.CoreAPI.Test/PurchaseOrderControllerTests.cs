using Invoice.BAL.Contracts;
using Invoice.CoreAPI.Controllers;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Invoice.CoreAPI.Test.Controllers;

public class PurchaseOrderControllerTests
{
    private readonly Mock<IPurchaseOrderService> _serviceMock = new();
    private readonly Mock<ILogger<PurchaseOrderController>> _loggerMock = new();
    private readonly PurchaseOrderController _controller;

    public PurchaseOrderControllerTests()
    {
        _controller = new PurchaseOrderController(_serviceMock.Object, _loggerMock.Object);

        ControllerTestHelpers.SetUser(_controller, "alice");
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        _serviceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<PurchaseOrderDto> { new() { Id = 1 } });

        Assert.IsType<OkObjectResult>(await _controller.GetAll());
    }

    [Fact]
    public async Task GetAll_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.GetAllAsync()).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.GetAll()).StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        _serviceMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new PurchaseOrderDto { Id = 1 });

        Assert.IsType<OkObjectResult>(await _controller.GetById(1));
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        _serviceMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((PurchaseOrderDto?)null);

        Assert.IsType<NotFoundObjectResult>(await _controller.GetById(1));
    }

    [Fact]
    public async Task Create_ReturnsOk_AndSetsCreatedByFromUser()
    {
        PurchaseOrderDto? sent = null;
        _serviceMock
            .Setup(x => x.AddAsync(It.IsAny<PurchaseOrderDto>()))
            .Callback<PurchaseOrderDto>(d => sent = d)
            .ReturnsAsync(5);

        var result = await _controller.Create(new PurchaseOrderDto());

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(5, Assert.IsType<ApiResponse<int>>(ok.Value).Data);
        Assert.Equal("alice", sent!.CreatedBy);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenThereAreNoLines()
    {
        _serviceMock
            .Setup(x => x.AddAsync(It.IsAny<PurchaseOrderDto>()))
            .ThrowsAsync(new BusinessRuleException("A purchase order must have at least one line."));

        Assert.IsType<BadRequestObjectResult>(await _controller.Create(new PurchaseOrderDto()));
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenPoNumberAlreadyExists()
    {
        _serviceMock
            .Setup(x => x.AddAsync(It.IsAny<PurchaseOrderDto>()))
            .ThrowsAsync(new BusinessRuleException("A record with the same unique value already exists."));

        Assert.IsType<BadRequestObjectResult>(await _controller.Create(new PurchaseOrderDto()));
    }

    [Fact]
    public async Task Update_ReturnsOk_AndUsesRouteIdAndUser()
    {
        PurchaseOrderDto? sent = null;
        _serviceMock
            .Setup(x => x.UpdateAsync(It.IsAny<PurchaseOrderDto>()))
            .Callback<PurchaseOrderDto>(d => sent = d)
            .ReturnsAsync(true);

        Assert.IsType<OkObjectResult>(await _controller.Update(9, new PurchaseOrderDto { Id = 1 }));
        Assert.Equal(9, sent!.Id);
        Assert.Equal("alice", sent.UpdatedBy);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        _serviceMock.Setup(x => x.UpdateAsync(It.IsAny<PurchaseOrderDto>())).ReturnsAsync(false);

        Assert.IsType<NotFoundObjectResult>(await _controller.Update(9, new PurchaseOrderDto()));
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenPoIsNotDraft()
    {
        _serviceMock
            .Setup(x => x.UpdateAsync(It.IsAny<PurchaseOrderDto>()))
            .ThrowsAsync(new BusinessRuleException("Only Draft purchase orders can be edited."));

        Assert.IsType<BadRequestObjectResult>(await _controller.Update(9, new PurchaseOrderDto()));
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenDeleted()
    {
        _serviceMock.Setup(x => x.DeleteAsync(3)).ReturnsAsync(true);

        Assert.IsType<OkObjectResult>(await _controller.Delete(3));
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        _serviceMock.Setup(x => x.DeleteAsync(3)).ReturnsAsync(false);

        Assert.IsType<NotFoundObjectResult>(await _controller.Delete(3));
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenPoIsApproved()
    {
        _serviceMock
            .Setup(x => x.DeleteAsync(3))
            .ThrowsAsync(new BusinessRuleException("Only Draft or Cancelled purchase orders can be deleted."));

        Assert.IsType<BadRequestObjectResult>(await _controller.Delete(3));
    }

    [Fact]
    public async Task GetAllPaged_ReturnsOk()
    {
        _serviceMock
            .Setup(x => x.GetAllPagedAsync("PO", 1, "Approved", 1, 10))
            .ReturnsAsync(new PagedResultDto<PurchaseOrderDto> { TotalRecords = 3 });

        var result = await _controller.GetAllPaged("PO", 1, "Approved");

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(3, Assert.IsType<ApiResponse<PagedResultDto<PurchaseOrderDto>>>(ok.Value).Data!.TotalRecords);
    }

    // ---------------- NEW: Approve / Cancel ----------------
    [Fact]
    public async Task Approve_ReturnsOk_AndPassesUser()
    {
        _serviceMock.Setup(x => x.ApproveAsync(4, "alice")).ReturnsAsync(true);

        Assert.IsType<OkObjectResult>(await _controller.Approve(4));
        _serviceMock.Verify(x => x.ApproveAsync(4, "alice"), Times.Once);
    }

    [Fact]
    public async Task Approve_ReturnsBadRequest_WhenPoIsNotDraft()
    {
        _serviceMock
            .Setup(x => x.ApproveAsync(4, "alice"))
            .ThrowsAsync(new BusinessRuleException("Only Draft purchase orders can be approved."));

        Assert.IsType<BadRequestObjectResult>(await _controller.Approve(4));
    }

    [Fact]
    public async Task Approve_ReturnsNotFound_WhenPoDoesNotExist()
    {
        _serviceMock.Setup(x => x.ApproveAsync(4, "alice")).ThrowsAsync(new NotFoundException("Purchase order not found."));

        Assert.IsType<NotFoundObjectResult>(await _controller.Approve(4));
    }

    [Fact]
    public async Task Approve_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.ApproveAsync(4, "alice")).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.Approve(4)).StatusCode);
    }

    [Fact]
    public async Task Cancel_ReturnsOk_AndPassesUser()
    {
        _serviceMock.Setup(x => x.CancelAsync(4, "alice")).ReturnsAsync(true);

        Assert.IsType<OkObjectResult>(await _controller.Cancel(4));
        _serviceMock.Verify(x => x.CancelAsync(4, "alice"), Times.Once);
    }

    [Fact]
    public async Task Cancel_ReturnsBadRequest_WhenPoHasReceipts()
    {
        _serviceMock
            .Setup(x => x.CancelAsync(4, "alice"))
            .ThrowsAsync(new BusinessRuleException("This purchase order has open receipts."));

        Assert.IsType<BadRequestObjectResult>(await _controller.Cancel(4));
    }

    [Fact]
    public async Task Cancel_ReturnsNotFound_WhenPoDoesNotExist()
    {
        _serviceMock.Setup(x => x.CancelAsync(4, "alice")).ThrowsAsync(new NotFoundException("Purchase order not found."));

        Assert.IsType<NotFoundObjectResult>(await _controller.Cancel(4));
    }

    [Fact]
    public async Task Cancel_Returns500_WhenServiceThrows()
    {
        _serviceMock.Setup(x => x.CancelAsync(4, "alice")).ThrowsAsync(new Exception("boom"));

        Assert.Equal(500, Assert.IsType<ObjectResult>(await _controller.Cancel(4)).StatusCode);
    }
}

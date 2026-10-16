using AutoMapper;
using Invoice.BAL.Common;
using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.BAL.Services;

public class ReceiptServiceEFSp : IReceiptService
{
    private readonly IReceiptRepository _repository;
    private readonly IReceiptDetailRepository _detailRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IPurchaseOrderDetailRepository _purchaseOrderDetailRepository;
    private readonly ITransactionRunner _transaction;
    private readonly IMapper _mapper;

    public ReceiptServiceEFSp(
        IReceiptRepository repository,
        IReceiptDetailRepository detailRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IPurchaseOrderDetailRepository purchaseOrderDetailRepository,
        ITransactionRunner transaction,
        IMapper mapper)
    {
        _repository = repository;
        _detailRepository = detailRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseOrderDetailRepository = purchaseOrderDetailRepository;
        _transaction = transaction;
        _mapper = mapper;
    }

    // ============================================================
    // CREATE
    // ============================================================
    public async Task<int> AddAsync(ReceiptDto dto)
    {
        if (dto.Details == null || dto.Details.Count == 0)
            throw new BusinessRuleException("A receipt must have at least one line.");

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(dto.PurchaseOrderId)
            ?? throw new NotFoundException("Purchase order not found.");

        if (purchaseOrder.Status is not ("Approved" or "PartiallyReceived"))
            throw new BusinessRuleException(
                "Goods can be received only against an Approved or PartiallyReceived purchase order.");

        var poLines = await LoadPurchaseOrderLinesAsync(purchaseOrder.Id);

        var entity = _mapper.Map<ReceiptEntity>(dto);

        entity.VendorId = purchaseOrder.VendorId;
        entity.Status = "Draft";
        entity.Details = BuildDetails(dto.Details, poLines);

        ApplyTotals(entity);

        return await _transaction.ExecuteAsync(async () =>
        {
            var id = await _repository.AddAsync(entity);

            foreach (var detail in entity.Details)
            {
                detail.ReceiptId = id;
                await _detailRepository.AddAsync(detail);
            }

            return id;
        });
    }

    // ============================================================
    // READ
    // ============================================================
    public async Task<IEnumerable<ReceiptDto>> GetAllAsync()
    {
        var entities = (await _repository.GetAllAsync()).ToList();

        foreach (var entity in entities)
            entity.Details = (await _detailRepository.GetByReceiptIdAsync(entity.Id)).ToList();

        return _mapper.Map<IEnumerable<ReceiptDto>>(entities);
    }

    public async Task<ReceiptDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);

        if (entity == null)
            return null;

        entity.Details = (await _detailRepository.GetByReceiptIdAsync(id)).ToList();

        return _mapper.Map<ReceiptDto>(entity);
    }

    public async Task<PagedResultDto<ReceiptDto>> GetAllPagedAsync(
        string? receiptNumber,
        int? purchaseOrderId,
        int? vendorId,
        string? status,
        int pageNumber,
        int pageSize)
    {
        var result = await _repository.GetAllPagedAsync(
            receiptNumber, purchaseOrderId, vendorId, status, pageNumber, pageSize);

        var entities = result.Data.ToList();

        foreach (var entity in entities)
            entity.Details = (await _detailRepository.GetByReceiptIdAsync(entity.Id)).ToList();

        return new PagedResultDto<ReceiptDto>
        {
            Data = _mapper.Map<IEnumerable<ReceiptDto>>(entities),
            TotalRecords = result.TotalRecords
        };
    }

    // ============================================================
    // UPDATE  (Draft only; PO, vendor and number cannot change)
    // ============================================================
    public async Task<bool> UpdateAsync(ReceiptDto dto)
    {
        var existing = await _repository.GetByIdAsync(dto.Id);

        if (existing == null)
            return false;

        if (existing.Status != "Draft")
            throw new BusinessRuleException("Only Draft receipts can be edited.");

        if (dto.Details == null || dto.Details.Count == 0)
            throw new BusinessRuleException("A receipt must have at least one line.");

        var poLines = await LoadPurchaseOrderLinesAsync(existing.PurchaseOrderId);

        var entity = _mapper.Map<ReceiptEntity>(dto);

        entity.ReceiptNumber = existing.ReceiptNumber;
        entity.PurchaseOrderId = existing.PurchaseOrderId;
        entity.VendorId = existing.VendorId;
        entity.Status = existing.Status;
        entity.Details = BuildDetails(dto.Details, poLines);

        ApplyTotals(entity);

        return await _transaction.ExecuteAsync(async () =>
        {
            var updated = await _repository.UpdateAsync(entity);

            if (!updated)
                return false;

            await _detailRepository.DeleteByReceiptIdAsync(entity.Id);

            foreach (var detail in entity.Details)
            {
                detail.ReceiptId = entity.Id;
                await _detailRepository.AddAsync(detail);
            }

            return true;
        });
    }

    // ============================================================
    // DELETE / WORKFLOW
    // ============================================================
    public Task<bool> DeleteAsync(int id, string? updatedBy = null)
        => _repository.DeleteAsync(id, updatedBy);

    public Task<bool> PostAsync(int id, string? updatedBy)
        => _repository.PostAsync(id, updatedBy);

    public Task<bool> CancelAsync(int id, string? updatedBy)
        => _repository.CancelAsync(id, updatedBy);

    // ============================================================
    // HELPERS
    // ============================================================
    private async Task<Dictionary<int, PurchaseOrderDetailEntity>> LoadPurchaseOrderLinesAsync(
        int purchaseOrderId)
    {
        var lines = await _purchaseOrderDetailRepository.GetByPurchaseOrderIdAsync(purchaseOrderId);

        return lines.ToDictionary(x => x.Id);
    }

    /// <summary>
    /// Item, rate, tax percent come from the PO line (never from the client).
    /// Discount is the PO line discount pro-rated by the received quantity.
    /// </summary>
    private static List<ReceiptDetailEntity> BuildDetails(
        List<ReceiptDetailDto> lines,
        Dictionary<int, PurchaseOrderDetailEntity> poLines)
    {
        if (lines.GroupBy(x => x.PurchaseOrderDetailId).Any(g => g.Count() > 1))
            throw new BusinessRuleException(
                "Each purchase order line can appear only once per receipt.");

        var result = new List<ReceiptDetailEntity>();

        for (var i = 0; i < lines.Count; i++)
        {
            var number = i + 1;
            var line = lines[i];

            if (!poLines.TryGetValue(line.PurchaseOrderDetailId, out var poLine))
                throw new BusinessRuleException(
                    $"Line {number}: purchase order line {line.PurchaseOrderDetailId} does not belong to this purchase order.");

            if (line.ReceivedQuantity <= 0)
                throw new BusinessRuleException(
                    $"Line {number}: received quantity must be greater than zero.");

            var outstanding = poLine.Quantity - poLine.ReceivedQuantity;

            if (line.ReceivedQuantity > outstanding)
                throw new BusinessRuleException(
                    $"Line {number}: received quantity {line.ReceivedQuantity:0.##} exceeds the outstanding quantity {outstanding:0.##}.");

            var discount = poLine.Quantity == 0
                ? 0m
                : LineCalculator.Round(poLine.DiscountAmount * line.ReceivedQuantity / poLine.Quantity);

            var amounts = LineCalculator.Calculate(
                line.ReceivedQuantity, poLine.Rate, discount, poLine.TaxPercent);

            result.Add(new ReceiptDetailEntity
            {
                PurchaseOrderDetailId = poLine.Id,
                ItemmasterId = poLine.ItemmasterId,
                ReceivedQuantity = line.ReceivedQuantity,
                Rate = poLine.Rate,
                DiscountAmount = discount,
                TaxPercent = poLine.TaxPercent,
                TaxAmount = amounts.TaxAmount,
                LineTotal = amounts.LineTotal
            });
        }

        return result;
    }

    private static void ApplyTotals(ReceiptEntity entity)
    {
        entity.SubTotal = entity.Details.Sum(d => d.LineTotal - d.TaxAmount);
        entity.TaxAmount = entity.Details.Sum(d => d.TaxAmount);
        entity.TotalAmount = entity.SubTotal + entity.TaxAmount;
    }
}

using AutoMapper;
using Invoice.BAL.Common;
using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.BAL.Services;

public class PurchaseOrderServiceEFSp : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IPurchaseOrderDetailRepository _detailRepository;
    private readonly ITransactionRunner _transaction;
    private readonly IMapper _mapper;

    public PurchaseOrderServiceEFSp(
        IPurchaseOrderRepository repository,
        IPurchaseOrderDetailRepository detailRepository,
        ITransactionRunner transaction,
        IMapper mapper)
    {
        _repository = repository;
        _detailRepository = detailRepository;
        _transaction = transaction;
        _mapper = mapper;
    }

    // ============================================================
    // CREATE  (always starts as Draft)
    // ============================================================
    public async Task<int> AddAsync(PurchaseOrderDto dto)
    {
        ValidateDetails(dto.Details);

        var entity = _mapper.Map<PurchaseOrderEntity>(dto);
        entity.Status = "Draft";

        CalculateTotals(entity);

        return await _transaction.ExecuteAsync(async () =>
        {
            var id = await _repository.AddAsync(entity);

            foreach (var detail in entity.Details)
            {
                detail.PurchaseOrderId = id;
                await _detailRepository.AddAsync(detail);
            }

            return id;
        });
    }

    // ============================================================
    // GET ALL
    // ============================================================
    public async Task<IEnumerable<PurchaseOrderDto>> GetAllAsync()
    {
        var entities = (await _repository.GetAllAsync()).ToList();

        foreach (var entity in entities)
        {
            var details = await _detailRepository.GetByPurchaseOrderIdAsync(entity.Id);
            entity.Details = details.ToList();
        }

        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(entities);
    }

    // ============================================================
    // GET BY ID
    // ============================================================
    public async Task<PurchaseOrderDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);

        if (entity == null)
            return null;

        var details = await _detailRepository.GetByPurchaseOrderIdAsync(id);
        entity.Details = details.ToList();

        return _mapper.Map<PurchaseOrderDto>(entity);
    }

    // ============================================================
    // UPDATE  (Draft only)
    // ============================================================
    public async Task<bool> UpdateAsync(PurchaseOrderDto dto)
    {
        var existing = await _repository.GetByIdAsync(dto.Id);

        if (existing == null)
            return false;

        if (existing.Status != "Draft")
            throw new BusinessRuleException("Only Draft purchase orders can be edited.");

        ValidateDetails(dto.Details);

        var entity = _mapper.Map<PurchaseOrderEntity>(dto);

        CalculateTotals(entity);

        return await _transaction.ExecuteAsync(async () =>
        {
            var updated = await _repository.UpdateAsync(entity);

            if (!updated)
                return false;

            await _detailRepository.DeleteByPurchaseOrderIdAsync(entity.Id);

            foreach (var detail in entity.Details)
            {
                detail.PurchaseOrderId = entity.Id;
                await _detailRepository.AddAsync(detail);
            }

            return true;
        });
    }

    // ============================================================
    // DELETE  (soft delete of the header; lines are kept for audit)
    // ============================================================
    public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);

    // ============================================================
    // PAGED
    // ============================================================
    public async Task<PagedResultDto<PurchaseOrderDto>> GetAllPagedAsync(
        string? PONumber,
        int? VendorId,
        string? Status,
        int pageNumber,
        int pageSize)
    {
        var result = await _repository.GetAllPagedAsync(
            PONumber, VendorId, Status, pageNumber, pageSize);

        var entities = result.Data.ToList();

        foreach (var entity in entities)
        {
            var details = await _detailRepository.GetByPurchaseOrderIdAsync(entity.Id);
            entity.Details = details.ToList();
        }

        return new PagedResultDto<PurchaseOrderDto>
        {
            Data = _mapper.Map<IEnumerable<PurchaseOrderDto>>(entities),
            TotalRecords = result.TotalRecords
        };
    }

    // ============================================================
    // WORKFLOW
    // ============================================================
    public Task<bool> ApproveAsync(int id, string? updatedBy)
        => _repository.ApproveAsync(id, updatedBy);

    public Task<bool> CancelAsync(int id, string? updatedBy)
        => _repository.CancelAsync(id, updatedBy);

    // ============================================================
    // HELPERS
    // ============================================================
    private static void ValidateDetails(List<PurchaseOrderDetailDto>? details)
    {
        if (details == null || details.Count == 0)
            throw new BusinessRuleException("A purchase order must have at least one line.");

        for (var i = 0; i < details.Count; i++)
        {
            var d = details[i];

            if (d.ItemmasterId <= 0)
                throw new BusinessRuleException($"Line {i + 1}: item is required.");

            LineCalculator.Validate(i + 1, d.Quantity, d.Rate, d.DiscountAmount, d.TaxPercent);
        }
    }

    private static void CalculateTotals(PurchaseOrderEntity entity)
    {
        decimal subTotal = 0;
        decimal taxAmount = 0;

        foreach (var detail in entity.Details)
        {
            var amounts = LineCalculator.Calculate(
                detail.Quantity, detail.Rate, detail.DiscountAmount, detail.TaxPercent);

            detail.TaxAmount = amounts.TaxAmount;
            detail.LineTotal = amounts.LineTotal;
            detail.ReceivedQuantity = 0;          // only receipts may change this

            subTotal += amounts.Taxable;
            taxAmount += amounts.TaxAmount;
        }

        entity.SubTotal = subTotal;
        entity.TaxAmount = taxAmount;
        entity.TotalAmount = subTotal + taxAmount;
    }
}

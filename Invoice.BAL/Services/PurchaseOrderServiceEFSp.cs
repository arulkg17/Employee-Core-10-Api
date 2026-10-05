using AutoMapper;
using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Services;

public class PurchaseOrderServiceEFSp : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IPurchaseOrderDetailRepository _detailRepository;
    private readonly IMapper _mapper;

    public PurchaseOrderServiceEFSp(
        IPurchaseOrderRepository repository,
        IPurchaseOrderDetailRepository detailRepository,
        IMapper mapper)
    {
        _repository = repository;
        _detailRepository = detailRepository;
        _mapper = mapper;
    }

    // ============================================================
    // CREATE
    // ============================================================
    public async Task<int> AddAsync(
        PurchaseOrderDto dto)
    {
        var entity =
            _mapper.Map<PurchaseOrderEntity>(dto);

        // Calculate totals on server
        CalculateTotals(entity);

        var id =
            await _repository.AddAsync(entity);

        // Add details
        if (dto.Details != null)
        {
            foreach (var detailDto in dto.Details)
            {
                var detail =
                    _mapper.Map<PurchaseOrderDetailEntity>(
                        detailDto);

                detail.PurchaseOrderId = id;

                await _detailRepository.AddAsync(detail);
            }
        }

        return id;
    }

    // ============================================================
    // GET ALL
    // ============================================================
    public async Task<IEnumerable<PurchaseOrderDto>>
        GetAllAsync()
    {
        var entities =
            await _repository.GetAllAsync();

        foreach (var entity in entities)
        {
            var details =
                await _detailRepository
                    .GetByPurchaseOrderIdAsync(entity.Id);

            entity.Details = details.ToList();
        }

        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(
            entities);
    }

    // ============================================================
    // GET BY ID
    // ============================================================
    public async Task<PurchaseOrderDto?>
        GetByIdAsync(int id)
    {
        var entity =
            await _repository.GetByIdAsync(id);

        if (entity == null)
            return null;

        var details =
            await _detailRepository
                .GetByPurchaseOrderIdAsync(id);

        entity.Details = details.ToList();

        return _mapper.Map<PurchaseOrderDto>(entity);
    }

    // ============================================================
    // UPDATE
    // ============================================================
    public async Task<bool> UpdateAsync(
        PurchaseOrderDto dto)
    {
        var entity =
            _mapper.Map<PurchaseOrderEntity>(dto);

        CalculateTotals(entity);

        var updated =
            await _repository.UpdateAsync(entity);

        if (!updated)
            return false;

        // Replace existing details
        await _detailRepository
            .DeleteByPurchaseOrderIdAsync(entity.Id);

        if (dto.Details != null)
        {
            foreach (var detailDto in dto.Details)
            {
                var detail =
                    _mapper.Map<PurchaseOrderDetailEntity>(
                        detailDto);

                detail.PurchaseOrderId = entity.Id;

                await _detailRepository.AddAsync(detail);
            }
        }

        return true;
    }

    // ============================================================
    // DELETE
    // ============================================================
    public async Task<bool> DeleteAsync(int id)
    {
        await _detailRepository
            .DeleteByPurchaseOrderIdAsync(id);

        return await _repository.DeleteAsync(id);
    }

    // ============================================================
    // PAGED
    // ============================================================
    public async Task<PagedResultDto<PurchaseOrderDto>>
        GetAllPagedAsync(
            string? PONumber,
            int? VendorId,
            string? Status,
            int PageNumber,
            int PageSize)
    {
        var result =
            await _repository.GetAllPagedAsync(
                PONumber,
                VendorId,
                Status,
                PageNumber,
                PageSize);

        var entities = result.Data.ToList();

        foreach (var entity in entities)
        {
            var details =
                await _detailRepository
                    .GetByPurchaseOrderIdAsync(entity.Id);

            entity.Details = details.ToList();
        }

        return new PagedResultDto<PurchaseOrderDto>
        {
            Data = _mapper.Map<IEnumerable<PurchaseOrderDto>>(
                entities),

            TotalRecords = result.TotalRecords
        };
    }

    // ============================================================
    // CALCULATE TOTALS
    // ============================================================
    private static void CalculateTotals(
        PurchaseOrderEntity entity)
    {
        decimal subTotal = 0;
        decimal taxAmount = 0;

        foreach (var detail in entity.Details)
        {
            var gross =
                detail.Quantity * detail.Rate;

            var lineTotalBeforeTax =
                gross - detail.DiscountAmount;

            detail.TaxAmount =
                lineTotalBeforeTax *
                detail.TaxPercent / 100;

            detail.LineTotal =
                lineTotalBeforeTax +
                detail.TaxAmount;

            subTotal += lineTotalBeforeTax;
            taxAmount += detail.TaxAmount;
        }

        entity.SubTotal = subTotal;
        entity.TaxAmount = taxAmount;
        entity.TotalAmount = subTotal + taxAmount;
    }
}
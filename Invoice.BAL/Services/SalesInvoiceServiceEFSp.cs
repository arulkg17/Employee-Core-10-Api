using AutoMapper;
using Invoice.BAL.Common;
using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.BAL.Services;

public class SalesInvoiceServiceEFSp : ISalesInvoiceService
{
    private readonly ISalesInvoiceRepository _repository;
    private readonly ISalesInvoiceDetailRepository _detailRepository;
    private readonly ITransactionRunner _transaction;
    private readonly IMapper _mapper;

    public SalesInvoiceServiceEFSp(
        ISalesInvoiceRepository repository,
        ISalesInvoiceDetailRepository detailRepository,
        ITransactionRunner transaction,
        IMapper mapper)
    {
        _repository = repository;
        _detailRepository = detailRepository;
        _transaction = transaction;
        _mapper = mapper;
    }

    // ============================================================
    // CREATE  (always starts as Draft; stock is checked on Post)
    // ============================================================
    public async Task<int> AddAsync(SalesInvoiceDto dto)
    {
        Validate(dto);

        var entity = _mapper.Map<SalesInvoiceEntity>(dto);
        entity.Status = "Draft";

        CalculateTotals(entity);

        return await _transaction.ExecuteAsync(async () =>
        {
            var id = await _repository.AddAsync(entity);

            foreach (var detail in entity.Details)
            {
                detail.SalesInvoiceId = id;
                await _detailRepository.AddAsync(detail);
            }

            return id;
        });
    }

    // ============================================================
    // READ
    // ============================================================
    public async Task<IEnumerable<SalesInvoiceDto>> GetAllAsync()
    {
        var entities = (await _repository.GetAllAsync()).ToList();

        foreach (var entity in entities)
            entity.Details = (await _detailRepository.GetBySalesInvoiceIdAsync(entity.Id)).ToList();

        return _mapper.Map<IEnumerable<SalesInvoiceDto>>(entities);
    }

    public async Task<SalesInvoiceDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);

        if (entity == null)
            return null;

        entity.Details = (await _detailRepository.GetBySalesInvoiceIdAsync(id)).ToList();

        return _mapper.Map<SalesInvoiceDto>(entity);
    }

    public async Task<PagedResultDto<SalesInvoiceDto>> GetAllPagedAsync(
        string? invoiceNumber,
        int? customerId,
        string? status,
        int pageNumber,
        int pageSize)
    {
        var result = await _repository.GetAllPagedAsync(
            invoiceNumber, customerId, status, pageNumber, pageSize);

        var entities = result.Data.ToList();

        foreach (var entity in entities)
            entity.Details = (await _detailRepository.GetBySalesInvoiceIdAsync(entity.Id)).ToList();

        return new PagedResultDto<SalesInvoiceDto>
        {
            Data = _mapper.Map<IEnumerable<SalesInvoiceDto>>(entities),
            TotalRecords = result.TotalRecords
        };
    }

    // ============================================================
    // UPDATE  (Draft only)
    // ============================================================
    public async Task<bool> UpdateAsync(SalesInvoiceDto dto)
    {
        var existing = await _repository.GetByIdAsync(dto.Id);

        if (existing == null)
            return false;

        if (existing.Status != "Draft")
            throw new BusinessRuleException("Only Draft sales invoices can be edited.");

        Validate(dto);

        var entity = _mapper.Map<SalesInvoiceEntity>(dto);

        entity.InvoiceNumber = existing.InvoiceNumber;
        entity.Status = existing.Status;

        CalculateTotals(entity);

        return await _transaction.ExecuteAsync(async () =>
        {
            var updated = await _repository.UpdateAsync(entity);

            if (!updated)
                return false;

            await _detailRepository.DeleteBySalesInvoiceIdAsync(entity.Id);

            foreach (var detail in entity.Details)
            {
                detail.SalesInvoiceId = entity.Id;
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
    private static void Validate(SalesInvoiceDto dto)
    {
        if (dto.CustomerId <= 0)
            throw new BusinessRuleException("Customer is required.");

        if (dto.DueDate.HasValue && dto.DueDate.Value < dto.InvoiceDate)
            throw new BusinessRuleException("Due date cannot be earlier than the invoice date.");

        if (dto.Details == null || dto.Details.Count == 0)
            throw new BusinessRuleException("A sales invoice must have at least one line.");

        for (var i = 0; i < dto.Details.Count; i++)
        {
            var d = dto.Details[i];

            if (d.ItemmasterId <= 0)
                throw new BusinessRuleException($"Line {i + 1}: item is required.");

            LineCalculator.Validate(i + 1, d.Quantity, d.Rate, d.DiscountAmount, d.TaxPercent);
        }
    }

    private static void CalculateTotals(SalesInvoiceEntity entity)
    {
        decimal subTotal = 0;
        decimal taxAmount = 0;

        foreach (var detail in entity.Details)
        {
            var amounts = LineCalculator.Calculate(
                detail.Quantity, detail.Rate, detail.DiscountAmount, detail.TaxPercent);

            detail.TaxAmount = amounts.TaxAmount;
            detail.LineTotal = amounts.LineTotal;

            subTotal += amounts.Taxable;
            taxAmount += amounts.TaxAmount;
        }

        entity.SubTotal = subTotal;
        entity.TaxAmount = taxAmount;
        entity.TotalAmount = subTotal + taxAmount;
    }
}

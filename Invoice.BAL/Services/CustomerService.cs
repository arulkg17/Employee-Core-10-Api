using AutoMapper;
using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customer;
    private readonly IMapper _mapper;

    public CustomerService(
        ICustomerRepository customer,
        IMapper mapper)
    {
        _customer = customer;
        _mapper = mapper;
    }

    public async Task<int> AddAsync(CustomerDto dto)
    {
        var entity = _mapper.Map<CustomerEntity>(dto);

        return await _customer.AddAsync(entity);
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var entities = await _customer.GetAllAsync();

        return _mapper.Map<IEnumerable<CustomerDto>>(entities);
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var entity = await _customer.GetByIdAsync(id);

        if (entity == null)
            return null;

        return _mapper.Map<CustomerDto>(entity);
    }

    public async Task<bool> UpdateAsync(CustomerDto dto)
    {
        var entity = _mapper.Map<CustomerEntity>(dto);

        return await _customer.UpdateAsync(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _customer.DeleteAsync(id);
    }

    public async Task<PagedResultDto<CustomerDto>> GetAllPagedAsync(
string? CustomerCode,
string? CustomerName,
string? MobileNo,
string? City,
int PageNumber,
int PageSize)
    {
        var result = await _customer.GetAllPagedAsync(
            CustomerCode,
            CustomerName,
            MobileNo,
            City,
            PageNumber,
            PageSize);

        return new PagedResultDto<CustomerDto>
        {
            Data = _mapper.Map<IEnumerable<CustomerDto>>(result.Data),
            TotalRecords = result.TotalRecords
        };
    }
}
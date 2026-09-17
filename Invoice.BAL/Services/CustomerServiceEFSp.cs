using AutoMapper;
using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Services;


public class CustomerServiceEFsp : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IMapper _mapper;
    public CustomerServiceEFsp(ICustomerRepository repository, IMapper mapper)
    {
        _repository = repository; _mapper = mapper;
    }
    public async Task<int> AddAsync(CustomerDto dto)
    {
        var entity = _mapper.Map<CustomerEntity>(dto);
        return await _repository.AddAsync(entity);
    }
    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var customers = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<CustomerDto>>(customers);
    }
    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var customers = await _repository.GetByIdAsync(id);
        return customers == null ? null : _mapper.Map<CustomerDto?>(customers);
    }
    public async Task<bool> UpdateAsync(CustomerDto dto)
    {
        var entiry = _mapper.Map<CustomerEntity>(dto);
        return await _repository.UpdateAsync(entiry);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }
    public async Task<PagedResultDto<CustomerDto>> GetAllPagedAsync(
        string? CustomerCode,
        string? CustomerName,
        string? MobileNo,
        string? City,
        int PageNumber,
        int PageSize
        )
    {
        var result = await _repository.GetAllPagedAsync(CustomerCode, CustomerName, MobileNo, City,
            PageNumber, PageSize);
        return new PagedResultDto<CustomerDto>
        {
            Data = _mapper.Map<IEnumerable<CustomerDto>>(result.Data),
            TotalRecords = result.TotalRecords

        };
    }
}
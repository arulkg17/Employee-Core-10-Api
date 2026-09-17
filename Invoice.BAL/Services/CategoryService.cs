using AutoMapper;
using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IMapper _mapper;
    public CategoryService(ICategoryRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
    public async Task<int> AddAsync(CategoryDto dto)
    {
        var entity = _mapper.Map<CategoryEntity>(dto);
        return await _repository.AddAsync(entity);
    }
    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<CategoryDto>>(items);
    }
    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : _mapper.Map<CategoryDto>(item);
    }
    public async Task<bool> UpdateAsync(CategoryDto dto)
    {
        var entity = _mapper.Map<CategoryEntity>(dto);
        return await _repository.UpdateAsync(entity);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }
    public async Task<PagedResultDto<CategoryDto>> GetAllPagedAsync(
string? catCode,
string? itemName,
int pageNumber,
int pageSize)
    {
        var result = await _repository.GetAllPagedAsync(
            catCode, itemName, pageNumber, pageSize);

        return new PagedResultDto<CategoryDto>
        {
            Data = _mapper.Map<IEnumerable<CategoryDto>>(result.Data),
            TotalRecords = result.TotalRecords
        };
    }
}
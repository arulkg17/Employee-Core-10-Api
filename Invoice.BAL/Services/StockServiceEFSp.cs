using AutoMapper;
using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.DTOs;

namespace Invoice.BAL.Services;

public class StockServiceEFSp : IStockService
{
    private readonly IStockRepository _repository;
    private readonly IMapper _mapper;

    public StockServiceEFSp(IStockRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ItemStockDto>> GetAllAsync()
    {
        var rows = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<ItemStockDto>>(rows);
    }

    public async Task<ItemStockDto?> GetByItemmasterIdAsync(int itemmasterId)
    {
        var row = await _repository.GetByItemmasterIdAsync(itemmasterId);
        return row == null ? null : _mapper.Map<ItemStockDto>(row);
    }
}

using AutoMapper;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Mapper;

public class StockProfile : Profile
{
    public StockProfile()
    {
        CreateMap<ItemStockEntity, ItemStockDto>();
    }
}

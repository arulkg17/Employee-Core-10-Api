using AutoMapper;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Mapper;

public class PurchaseOrderProfile : Profile
{
    public PurchaseOrderProfile()
    {
        CreateMap<PurchaseOrderEntity, PurchaseOrderDto>()
            .ReverseMap();

        CreateMap<PurchaseOrderDetailEntity, PurchaseOrderDetailDto>()
            .ReverseMap();
    }
}
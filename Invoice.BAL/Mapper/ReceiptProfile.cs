using AutoMapper;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Mapper;

public class ReceiptProfile : Profile
{
    public ReceiptProfile()
    {
        CreateMap<ReceiptEntity, ReceiptDto>().ReverseMap();
        CreateMap<ReceiptDetailEntity, ReceiptDetailDto>().ReverseMap();
    }
}

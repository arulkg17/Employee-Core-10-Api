using AutoMapper;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Mapper;

public class SalesInvoiceProfile : Profile
{
    public SalesInvoiceProfile()
    {
        CreateMap<SalesInvoiceEntity, SalesInvoiceDto>().ReverseMap();
        CreateMap<SalesInvoiceDetailEntity, SalesInvoiceDetailDto>().ReverseMap();
    }
}

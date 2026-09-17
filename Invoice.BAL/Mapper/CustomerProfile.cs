using AutoMapper;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Mapper;

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<CustomerEntity, CustomerDto>().ReverseMap();
    }
}
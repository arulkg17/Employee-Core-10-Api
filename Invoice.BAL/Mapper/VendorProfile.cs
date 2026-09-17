using AutoMapper;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Mapper;

public class VendorProfile : Profile
{
    public VendorProfile()
    {
        CreateMap<VendorEntity, VendorDto>().ReverseMap();
    }
}
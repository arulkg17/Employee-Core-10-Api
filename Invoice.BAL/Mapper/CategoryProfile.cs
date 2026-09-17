using Invoice.Data.Entities;
using Invoice.DTOs;
using AutoMapper;
namespace Invoice.BAL.Mapper;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<CategoryEntity, CategoryDto>().ReverseMap();
    }
}
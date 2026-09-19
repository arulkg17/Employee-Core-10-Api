using AutoMapper;
using Invoice.Data.Entities;
using Invoice.DTOs;

namespace Invoice.BAL.Mapper;

public class UserProfile : Profile 
{
    public UserProfile()
    {
        CreateMap<UserEntity, UserDto>().ReverseMap();
    }
}


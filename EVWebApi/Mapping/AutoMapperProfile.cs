using AutoMapper;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Role;
using EVWebApi.DTOs.User;
using EVWebApi.Models;

namespace EVWebApi.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName));


            CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());


            CreateMap<UpdateUserDto, User>();
            CreateMap<Group, GroupDto>();

            CreateMap<Role, RoleDto>();
        }
    }
}

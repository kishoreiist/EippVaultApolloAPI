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
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role == null ? string.Empty : src.Role.RoleName))
                .ForMember(dest => dest.Status,
               opt => opt.MapFrom(src => src.Status == UserStatus.active))


            .ForMember(dest => dest.GroupIds,
               opt => opt.MapFrom(src => src.UserGroups.Select(ug => ug.GroupId).ToList()))
            .ForMember(dest => dest.GroupNames,
               opt => opt.MapFrom(src =>
                    src.UserGroups
                     .Where(ug => ug.Group != null)
                    .Select(ug => ug.Group.GroupName).ToList()
                ));




            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status ? UserStatus.active : UserStatus.inactive));

            CreateMap<UpdateUserDto, User>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status
                     ? UserStatus.active
                     : UserStatus.inactive));

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

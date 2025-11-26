using AutoMapper;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.User;
using EVWebApi.Models;

namespace EVWebApi.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>()
            
                .ForMember(dest => dest.Status,
               opt => opt.MapFrom(src => src.Status == UserStatus.active))


            .ForMember(dest => dest.GroupId,
               opt => opt.MapFrom(src => src.UserGroup != null ? src.UserGroup.GroupId : (int?)null))
            .ForMember(dest => dest.GroupName,
               opt => opt.MapFrom(src =>
                    src.UserGroup != null && src.UserGroup.Group != null
                    ? src.UserGroup.Group.GroupName
                    : null));
                

            CreateMap<UpdateUserDto, User>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status
                     ? UserStatus.active
                     : UserStatus.inactive));

            CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status ? UserStatus.active : UserStatus.inactive))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());


            CreateMap<UpdateUserDto, User>();


            CreateMap<Group, GroupDto>();
            CreateMap<CreateGroupDto, Group>()
                 .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdateGroupDto, Group>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()); 



        }
    }
}

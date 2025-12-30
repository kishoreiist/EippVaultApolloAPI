using AutoMapper;
using EVWebApi.DTOs.Audit;
using EVWebApi.DTOs.Cabinet;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.User;
using EVWebApi.Models;
using System;

namespace EVWebApi.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            //USER
            CreateMap<User, UserDto>()
             .ForMember(dest => dest.Status,
               opt => opt.MapFrom(src => src.Status == UserStatus.active))

            .ForMember(dest => dest.GroupId,
               opt => opt.MapFrom(src => src.UserGroup != null ? src.UserGroup.GroupId : (int?)null))

            .ForMember(dest => dest.GroupName,
               opt => opt.MapFrom(src =>
                    src.UserGroup != null && src.UserGroup.Group != null
                    ? src.UserGroup.Group.GroupName
                    : null))

            .ForMember(dest => dest.UserType,
                opt => opt.MapFrom(src =>
                    src.UserGroup != null && src.UserGroup.Group != null
                    ? src.UserGroup.Group.UserType
                    : null))


            .ForMember(dest => dest.AccessList,
                opt => opt.MapFrom(src =>
                    src.UserGroup.Group.GroupAccessRights 
                     
                            .Select(x => new ListDto
                            {
                                Id=x.AccessRight.Id,
                                Name=x.AccessRight.AccessName
                            })
                            .ToList()
                       ))

            .ForMember(dest => dest.CabinetsList,
                opt => opt.MapFrom(src =>
                    src.UserGroup.Group.GroupCabinets 
                            .Select(x => new ListDto
                            {
                                Id = x.Cabinet.CabinetId,
                                Name = x.Cabinet.CabinetName
                            })
                            .ToList()));

            CreateMap<UpdateUserDto, User>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status
                     ? UserStatus.active
                     : UserStatus.inactive))
                .ForAllMembers(opt => opt.Condition(
                (src, dest, srcValue) => srcValue != null));

            CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status ? UserStatus.active : UserStatus.inactive))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());


            //GROUP
            CreateMap<Group, GroupDto>()
                .ForMember(dest => dest.AccessList,
                opt => opt.MapFrom(src =>
                    src.GroupAccessRights

                            .Select(x => new ListDto
                            {
                                Id = x.AccessRight.Id,
                                Name = x.AccessRight.AccessName
                            })
                            .ToList()
                       ))

            .ForMember(dest => dest.CabinetsList,
                opt => opt.MapFrom(src =>
                    src.GroupCabinets
                            .Select(x => new ListDto
                            {
                                Id = x.Cabinet.CabinetId,
                                Name = x.Cabinet.CabinetName
                            })
                            .ToList()));



            CreateMap<CreateGroupDto, Group>()
                 .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdateGroupDto, Group>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcValue) => srcValue != null));
           
            //CABINET
            CreateMap<Cabinet, CabinetDto>();
            CreateMap<CreateCabinetDto, Cabinet>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
            CreateMap<UpdateCabinetDto, Cabinet>()
                .ForAllMembers(opt => opt.Condition(
                    (src, dest, srcValue) => srcValue != null));

            CreateMap<Document, DocumentResponseDto>()
                .ForMember(
                dest => dest.Metadata,
                opt => opt.MapFrom(src => src.MetadataList)
                )
                 .ForMember(dest => dest.NotesCount,
               opt => opt.MapFrom(src => src.Notes.Count))

                 .ForMember(type => type.DocumentType,
                        opt => opt.MapFrom(src => src.DocumentType != null
                            ? src.DocumentType.Label
                            : null))
                .ReverseMap();


   



            CreateMap<Metadata, MetadataDTO>()
            .ForMember(
                dest => dest.Key,
                opt => opt.MapFrom(src => src.MetaKey)
            )
            .ForMember(
                dest => dest.Value,
                opt => opt.MapFrom(src => src.MetaValue)
            );
            CreateMap<Document, UpdateDocumentDto>()
                .ForAllMembers(opt => opt.Condition(
                    (src, dest, srcValue) => srcValue != null));


            CreateMap<AuditLog, AuditLogDTO>();
            CreateMap<Notes, NotesDto>();
        }
    }
}

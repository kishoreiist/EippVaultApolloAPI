using AutoMapper;
using EVWebApi.DTOs;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.AspNetCore.Identity;

namespace EVWebApi.Services
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GroupService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
        public async Task<IEnumerable<GroupDto>> GetAllAsync()
        {
            var groups = await _uow.Groups.GetAllAsync();
            return _mapper.Map<IEnumerable<GroupDto>>(groups);
        }


        public async Task<GroupDto> GetByIdAsync(int id)
        {
            var group = await _uow.Groups.GetByIdAsync(id);
            if (group == null) return null;
            return _mapper.Map<GroupDto>(group);
        }

        public async Task<GroupDto> CreateAsync(GroupDto dto)
        {
            var exists = await _uow.Groups.GetByGroupnameAsync(dto.GroupName);
            if (exists != null) throw new ArgumentException("Group name already exists");


            var group = new Group
            {
                GroupName = dto.GroupName,
                Description = dto.Description
            };
            await _uow.Groups.AddAsync(group);
            await _uow.CompleteAsync();


            return _mapper.Map<GroupDto>(group);
        }

        public async Task<GroupDto> UpdateAsync(GroupDto dto)
        {
            var group = await _uow.Groups.GetByIdAsync(dto.GroupId);
            if (group == null) throw new ArgumentException("Group not found");


            if (!string.IsNullOrWhiteSpace(dto.GroupName)) group.GroupName = dto.GroupName;
            if (!string.IsNullOrWhiteSpace(dto.Description)) group.Description = dto.Description; 
            //group.UpdatedAt = DateTime.UtcNow;


            _uow.Groups.Update(group);
            await _uow.CompleteAsync();


            return _mapper.Map<GroupDto>(group);
        }
        public async Task DeleteAsync(int id)
        {
            var group = await _uow.Groups.GetByIdAsync(id);
            if (group == null) throw new ArgumentException("Group not found");


            _uow.Groups.Remove(group);
            await _uow.CompleteAsync();
        }

    }
}

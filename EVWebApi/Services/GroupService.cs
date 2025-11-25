using AutoMapper;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        public async Task<PagedResponse<GroupDto>> GetAllAsync(GroupQueryParameters query)
        {

            var groupsQuery = _uow.Groups.Query();

            // search (name + description)
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.ToLower();
                groupsQuery = groupsQuery.Where(g =>
                    g.GroupName.ToLower().Contains(search) ||
                    (g.Description != null && g.Description.ToLower().Contains(search))
                );
            }

            //  Date Range
            if (query.FromDate.HasValue)
            {
                groupsQuery = groupsQuery.Where(g => g.CreatedAt >= query.FromDate.Value);
            }
            if (query.ToDate.HasValue)
            {
                groupsQuery = groupsQuery.Where(g => g.CreatedAt <= query.ToDate.Value);
            }

            //  Pagination 
            int totalCount =  groupsQuery.Count();

            var items = groupsQuery
                .Skip(query.Offset)
                .Take(query.Limit)
                .ToList();

            var mapped = _mapper.Map<List<GroupDto>>(items);

            return new PagedResponse<GroupDto>
            {
                Data = mapped,
                TotalRecords = totalCount,
                Offset = query.Offset,
                Limit = query.Limit
            };
        }


        public async Task<GroupDto> GetByIdAsync(int id)
        {
            var group = await _uow.Groups.GetByIdAsync(id);
            if (group == null) 
                throw new NotFoundException("Group not found");
            return _mapper.Map<GroupDto>(group);
        }

        public async Task<GroupDto> CreateAsync(GroupDto dto)
        {
            var exists = await _uow.Groups.GetByGroupnameAsync(dto.GroupName);
            if (exists != null) 
                throw new ConflictException($"GroupName '{dto.GroupName}' already exists");


            var group = new Group
            {
                GroupName = dto.GroupName,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                //UpdatedAt = DateTime.UtcNow
            };
            await _uow.Groups.AddAsync(group);
            await _uow.CompleteAsync();


            return _mapper.Map<GroupDto>(group);
        }

        public async Task<GroupDto> UpdateAsync(GroupDto dto)
        {
            var group = await _uow.Groups.GetByIdAsync(dto.GroupId);
            if (group == null) 
                throw new NotFoundException("Group not found");


            if (!string.IsNullOrWhiteSpace(dto.GroupName)) group.GroupName = dto.GroupName;
            if (dto.Description!=null)
                group.Description = dto.Description; 
            //group.UpdatedAt = DateTime.UtcNow;


            _uow.Groups.Update(group);
            await _uow.CompleteAsync();


            return _mapper.Map<GroupDto>(group);
        }
        public async Task DeleteAsync(int id)
        {
            var group = await _uow.Groups.GetByIdAsync(id);
            if (group == null) 
                throw new NotFoundException("Group not found");


            _uow.Groups.Remove(group);
            await _uow.CompleteAsync();
        }

    }
}

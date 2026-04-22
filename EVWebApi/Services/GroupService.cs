using AutoMapper;
using DocumentFormat.OpenXml.InkML;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;
using EVWebApi.Exceptions;
using EVWebApi.Helpers.ExportToExcel;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            //group name

            if (!string.IsNullOrWhiteSpace(query.Groupname))
            {
                string groupname = query.Groupname.ToLower();
                groupsQuery = groupsQuery.Where(g =>
                    g.GroupName.ToLower().Contains(groupname)
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
            int totalRecords = await groupsQuery.CountAsync();

            // If pageSize is invalid, normalize it
            if (query.PageSize <= 0)
                query.PageSize = 10;

            // Calculate total pages
            int totalPages = (int)Math.Ceiling(totalRecords / (double)query.PageSize);

            // Normalize pageNumber
            if (query.PageNumber <= 0)
                query.PageNumber = 1;

            if (query.PageNumber > totalPages && totalPages > 0)
                query.PageNumber = totalPages;


            var items = groupsQuery
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

            var mapped = _mapper.Map<List<GroupDto>>(items);

            return new PagedResponse<GroupDto>
            {
                Data = mapped,
                TotalRecords = totalRecords,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }


        public async Task<GroupDto> GetByIdAsync(int id)
        {
            var group = await _uow.Groups.GetByIdAsync(id);
            if (group == null)
                throw new NotFoundException("Group not found");
            return _mapper.Map<GroupDto>(group);
        }

        public async Task<GroupDto> CreateAsync(CreateGroupDto dto)
        {
            var exists = await _uow.Groups.GetByGroupnameAsync(dto.GroupName);
            if (exists != null)
                throw new ConflictException($"GroupName '{dto.GroupName}' already exists");

            var group = _mapper.Map<Group>(dto);

            group.GroupAccessRights = new List<GroupAccessRight>();
            group.GroupCabinets = new List<GroupCabinet>();

            if (dto.AccessList?.Any() == true)
            {
                foreach (var accessId in dto.AccessList.Distinct())
                {
                    group.GroupAccessRights.Add(new GroupAccessRight
                    {
                        AccessId = accessId
                    });
                }
            }

            if (dto.CabinetsList?.Any() == true)
            {
                foreach (var cabinetId in dto.CabinetsList.Distinct())
                {
                    group.GroupCabinets.Add(new GroupCabinet
                    {
                        CabinetId = cabinetId
                    });
                }
            }
            //var group = new Group
            //{
            //    GroupName = dto.GroupName,
            //    GroupAccessRights = new List<GroupAccessRight>(),
            //    GroupCabinets = new List<GroupCabinet>(),
            //    //Description = dto.Description,
            //    UserType = dto.UserType,
            //    CreatedAt = DateTime.UtcNow,

            //};
            await _uow.Groups.AddAsync(group);
            await _uow.CompleteAsync();

            var savedGroup = await _uow.Groups.GetByIdAsync(group.GroupId);
            return _mapper.Map<GroupDto>(savedGroup);
        }

        public async Task<GroupDto> UpdateAsync(UpdateGroupDto dto)
        {
            var group = await _uow.Groups.GetByIdAsync(dto.GroupId);
            if (group == null)
                throw new NotFoundException("Group not found");


            if (!string.IsNullOrWhiteSpace(dto.GroupName)) group.GroupName = dto.GroupName;
            //if (dto.Description != null)
            //    group.Description = dto.Description;
            if (!string.IsNullOrWhiteSpace(dto.UserType)) group.UserType = dto.UserType;
            if (dto.CabinetsList != null && dto.CabinetsList.Count > 0)
            {
                group.GroupCabinets.Clear();
                foreach (var cabinetId in dto.CabinetsList)
                {
                    group.GroupCabinets.Add(new GroupCabinet
                    {
                        CabinetId = cabinetId,
                        GroupId = group.GroupId
                    });
                }
            }
            if (dto.AccessList != null && dto.AccessList.Count > 0)
            {
                group.GroupAccessRights.Clear();
                foreach (var accessId in dto.AccessList)
                {
                    group.GroupAccessRights.Add(new GroupAccessRight
                    {
                        AccessId = accessId,
                        GroupId = group.GroupId
                    });
                }
            }

            _uow.Groups.Update(group);
            await _uow.CompleteAsync();
            var editedGroup = await _uow.Groups.GetByIdAsync(group.GroupId);

            return _mapper.Map<GroupDto>(editedGroup);
        }
        public async Task DeleteAsync(int id)
        {
            var group = await _uow.Groups.GetByIdAsync(id);
            if (group == null)
                throw new NotFoundException("Group not found");

            var hasUsers = await _uow.Groups.GetUsersAsync(id);
            if (hasUsers)
                throw new ConflictException(
                     "Cannot delete the group because it is assigned to one or more users."
                 );

            _uow.Groups.Remove(group);
            await _uow.CompleteAsync();


        }

        public async Task<List<GroupListDto>> GetGroupsForDropdownAsync()
        {
            var groups = await _uow.Groups.GetGroupsForDropdownAsync();
            return groups;
        }


        public async Task<(byte[], string)> GroupsExportToExcel(GroupQueryParameters query)
        {

            // force no pagination
            query.PageNumber = 1;


            var usersQuery = ApplyGroupFilters(query);
            var users = await usersQuery
                .AsNoTracking()
                .ToListAsync();
            var columns = new List<string>
            {
                "GroupName",
                "UserType",
                "AccessList",
                "CabinetList",
                "CreatedAt"

            };

            var excel = ExportExcelBuildHelper.BuildExcel(
                users,
                columns,
                (u, col) => ExcelColumnsHelper.GetGroupColumnValue(u, col)
            );

            return (excel, $"Groups__{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.xlsx");
        }
        //--------------------email grp--------------------------------------
        public async Task<EmailGroupDto> CreateEmailGroupAsync(CreateEmailGroupDto dto)
        {
            var exists = await _uow.Groups.GetByEmailGroupnameAsync(dto.GroupName);
            if (exists != null)
                throw new ConflictException($"Email group with '{dto.GroupName}' name already exists");
            var emailGroup = new EmailGroup
            {
                GroupName = dto.GroupName,
                IsExternal = dto.IsExternal,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Groups.AddEmailGroupAsync(emailGroup);
            await _uow.CompleteAsync();
            return _mapper.Map<EmailGroupDto>(emailGroup);
        }

        public async Task<List<EmailGroup>> GetallEmailGroupForDropDownAsync()
        {
            var emailGroup = await _uow.Groups.GeAllEmailGroupsAsync();
            return emailGroup;
        }

        public async Task<EmailGroupDto> UpdateEmailGroupAsync(EmailGroupDto emailGroup)
        {
            var existingGroup = await _uow.Groups.GetEmailGroupByIdAsync(emailGroup.Id);
            if (existingGroup == null)
                throw new NotFoundException("Email group not found");

            var conflictGroup = await _uow.Groups.GetByEmailGroupnameAsync(emailGroup.GroupName);
            if (conflictGroup != null && conflictGroup.Id != emailGroup.Id)
                throw new ConflictException($"Email group with '{emailGroup.GroupName}' name already exists");

            existingGroup.GroupName = emailGroup.GroupName;
            existingGroup.IsExternal = emailGroup.IsExternal;
            _uow.Groups.UpdateEmailGroupAsync(existingGroup);
            await _uow.CompleteAsync();
            return _mapper.Map<EmailGroupDto>(existingGroup);
        }

        public async Task DeleteEmailGroupAsync(int id)
        {
            var emailGroup = await _uow.Groups.GetEmailGroupByIdAsync(id);
            if (emailGroup == null)
                throw new NotFoundException("Email group not found");
            _uow.Groups.RemoveEmailGroupAsync(emailGroup);
            await _uow.CompleteAsync();
        }


        //-----------------------------hlpr methods-------------------

        private IQueryable<Group> ApplyGroupFilters(GroupQueryParameters query)
        {
            var groupsQuery = _uow.Groups.Query();
            //group name

            if (!string.IsNullOrWhiteSpace(query.Groupname))
            {
                string groupname = query.Groupname.ToLower();
                groupsQuery = groupsQuery.Where(g =>
                            g.GroupName.ToLower().Contains(groupname)
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

            return groupsQuery;
        }
    }
}

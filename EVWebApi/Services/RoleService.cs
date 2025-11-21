using AutoMapper;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.Role;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using System.Collections.Generic;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace EVWebApi.Services
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public RoleService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<PagedResponse<RoleDto>> GetAllAsync(RoleQueryParameters query)
        {
            //var roles = await _uow.Roles.GetAllAsync();
            //return _mapper.Map<IEnumerable<RoleDto>>(roles);


            var rolesQuery = _uow.Roles.Query();

            // search - name 
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var keyword = query.Search.ToLower();

                rolesQuery = rolesQuery.Where(u =>
                    u.RoleName.ToLower().Contains(keyword)
                );
            }

            //permission 

            if (!string.IsNullOrWhiteSpace(query.PermissionKey))
            {
                var requiredPermissionFilter = new Dictionary<string, bool>
                { 
                { query.PermissionKey, true }
                };
                var filter = JsonSerializer.Serialize(requiredPermissionFilter);
                rolesQuery = rolesQuery.Where(r =>
                   EF.Functions.JsonContains(r.Permissions, filter));
            }
            //  Date Range
            if (query.FromDate.HasValue)
            {
                rolesQuery = rolesQuery.Where(r => r.CreatedAt >= query.FromDate.Value);
            }
            if (query.ToDate.HasValue)
            {
                rolesQuery = rolesQuery.Where(r => r.CreatedAt <= query.ToDate.Value);
            }

            //  Pagination 
            int totalCount = rolesQuery.Count();

            var items = rolesQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var mapped = _mapper.Map<List<RoleDto>>(items);

            return new PagedResponse<RoleDto>
            {
                Data = mapped,
                TotalRecords = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }
        public async Task<RoleDto> GetByIdAsync(int id)
        {
            var role = await _uow.Roles.GetByIdAsync(id);
            if (role == null) return null;
            return _mapper.Map<RoleDto>(role);
        }

        public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
        {
            var exists = await _uow.Roles.GetByNameAsync(dto.RoleName);
            if (exists != null) throw new ArgumentException("Role name already exists");

            var role = new Role
            {
                RoleName = dto.RoleName,
                Permissions = dto.Permissions,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _uow.Roles.AddAsync(role);
            await _uow.CompleteAsync();
            return _mapper.Map<RoleDto>(role);

        }
        public async Task<RoleDto> UpdateAsync(UpdateRoleDto dto)
        {
            var role = await _uow.Roles.GetByIdAsync(dto.RoleId);
            if (role == null) throw new ArgumentException("Role not found");


            if (!string.IsNullOrWhiteSpace(dto.RoleName)) role.RoleName = dto.RoleName;


            if (dto.Permissions != null)
                role.Permissions = dto.Permissions;

            role.UpdatedAt = DateTime.UtcNow;
            _uow.Roles.Update(role);
            await _uow.CompleteAsync();


            return _mapper.Map<RoleDto>(role);
        }

        public async Task DeleteAsync(int id)
        {
            var role = await _uow.Roles.GetByIdAsync(id);
            if (role == null) throw new ArgumentException("Role not found");


            _uow.Roles.Remove(role);
            await _uow.CompleteAsync();
        }
    }
}

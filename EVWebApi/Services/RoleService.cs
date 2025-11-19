using AutoMapper;
using EVWebApi.DTOs;
using EVWebApi.Interfaces;
using EVWebApi.Models;
using Microsoft.AspNetCore.Identity;

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

        public async Task<IEnumerable<RoleDto>> GetAllAsync()
        {
            var roles = await _uow.Roles.GetAllAsync();
            return _mapper.Map<IEnumerable<RoleDto>>(roles);
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
                RoleName = dto.RoleName
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

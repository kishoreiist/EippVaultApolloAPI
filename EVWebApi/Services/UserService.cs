using AutoMapper;
using EVWebApi.DTOs;
using EVWebApi.Interfaces;
using EVWebApi.Models;
using Microsoft.AspNetCore.Identity;

namespace EVWebApi.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;


        public UserService(IUnitOfWork uow, IMapper mapper, IPasswordHasher<User> passwordHasher)
        {
            _uow = uow;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
        }


        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _uow.Users.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }


        public async Task<UserDto> GetByIdAsync(int id)
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user == null) return null;
            return _mapper.Map<UserDto>(user);
        }
        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var exists = await _uow.Users.GetByUsernameAsync(dto.Username);
            if (exists != null) throw new ArgumentException("Username already exists");


            var emailExists = await _uow.Users.GetByEmailAsync(dto.Email);
            if (emailExists != null) throw new ArgumentException("Email already exists");


            var user = new User
            {
                Username = dto.Username,
                PasswordHash = dto.Password,
                Email = dto.Email,
                RoleId = dto.RoleId,
                MfaEnabled = dto.MfaEnabled,
                
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserGroups = dto.GroupIds.Select(gid => new UserGroup
                {
                    GroupId = gid
                }).ToList()
            };


            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);


            await _uow.Users.AddAsync(user);
            await _uow.CompleteAsync();


            return _mapper.Map<UserDto>(user);
        }


        public async Task<UserDto> UpdateAsync(UpdateUserDto dto)
        {
            var user = await _uow.Users.GetByIdAsync(dto.UserId);
            if (user == null) throw new ArgumentException("User not found");


            if (!string.IsNullOrWhiteSpace(dto.Username)) user.Username = dto.Username;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
            if (dto.RoleId.HasValue) user.RoleId = dto.RoleId.Value;
            if (dto.Status.HasValue) user.Status = dto.Status.Value;
            if (dto.MfaEnabled.HasValue) user.MfaEnabled = dto.MfaEnabled.Value;
            if (dto.MfaMethod.HasValue) user.MfaMethod = dto.MfaMethod;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;
            if (dto.EmailVerified.HasValue) user.EmailVerified = dto.EmailVerified.Value;


            user.UpdatedAt = DateTime.UtcNow;


            _uow.Users.Update(user);
            await _uow.CompleteAsync();


            return _mapper.Map<UserDto>(user);
        }


        public async Task DeleteAsync(int id)
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user == null) throw new ArgumentException("User not found");


            _uow.Users.Remove(user);
            await _uow.CompleteAsync();
        }
    }
}
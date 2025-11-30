using AutoMapper;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EVWebApi.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
 


        public UserService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }


        public async Task<PagedResponse<UserDto>> GetAllAsync(UserQueryParameters query)
        {
            var usersQuery = _uow.Users.Query();

            if (!string.IsNullOrWhiteSpace(query.Username))
                usersQuery = usersQuery.Where(u => u.Username.Contains(query.Username));

            if (!string.IsNullOrWhiteSpace(query.Email))
                usersQuery = usersQuery.Where(u => u.Email.Contains(query.Email));

            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                usersQuery = usersQuery.Where(u => u.PhoneNumber.Contains(query.PhoneNumber));


            if (!string.IsNullOrWhiteSpace(query.GroupName))
                usersQuery = usersQuery.Where(u => u.UserGroup != null &&
                             u.UserGroup.Group.GroupName.ToLower().Contains(query.GroupName.ToLower()));

            //  TOTAL count BEFORE pagination
            var totalRecords = usersQuery.Count();

            // APPLY PAGINATION
            var pagedUsers = usersQuery
                .Skip(query.Offset)
                .Take(query.Limit)
                .ToList();

            // MAP TO DTO
            var userDtos = _mapper.Map<List<UserDto>>(pagedUsers);
            return new PagedResponse<UserDto>
            {
                Data = userDtos,
                TotalRecords = totalRecords,
                Offset = query.Offset,
                Limit = query.Limit
            };
        }


        public async Task<UserDto> GetByIdAsync(int id)
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user == null)
                throw new NotFoundException($"User with id {id} not found");

            return _mapper.Map<UserDto>(user);
        }
        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var exists = await _uow.Users.GetByUsernameAsync(dto.Username);
            if (exists != null)
                throw new ConflictException($"Username '{dto.Username}' already exists");


            var emailExists = await _uow.Users.GetByEmailAsync(dto.Email);
            if (emailExists != null) 
                throw new ConflictException($"User mail '{dto.Email}' already exists");

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Email = dto.Email,
                MfaEnabled = dto.MfaEnabled,
                Status = dto.Status ? UserStatus.active : UserStatus.inactive,
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserGroup = new UserGroup
                {
                    GroupId = dto.GroupId
                }
            };

            await _uow.Users.AddAsync(user);
            await _uow.CompleteAsync();

            var updatedUser = await _uow.Users.GetByIdAsync(user.UserId);

            return _mapper.Map<UserDto>(updatedUser);
        }


        public async Task<UserDto> UpdateAsync(UpdateUserDto dto)
        {
            var user = await _uow.Users.GetByIdAsync(dto.UserId);
            if (user == null) 
                throw new NotFoundException($"User with id {dto.UserId} not found");


            if (!string.IsNullOrWhiteSpace(dto.Username)) user.Username = dto.Username;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
            if (dto.MfaEnabled.HasValue) user.MfaEnabled = dto.MfaEnabled.Value;
            if (dto.MfaMethod.HasValue) user.MfaMethod = dto.MfaMethod;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;
            if (dto.EmailVerified.HasValue) user.EmailVerified = dto.EmailVerified.Value;
            if (dto.Status)
            {
                user.Status = dto.Status
                    ? UserStatus.active
                    : UserStatus.inactive;
            }

            if (dto.GroupId != 0) user.UserGroup.GroupId = dto.GroupId;
            user.UpdatedAt = DateTime.UtcNow;


            _uow.Users.Update(user);
            await _uow.CompleteAsync();
            var updatedUser = await _uow.Users.GetByIdAsync(user.UserId);

            return _mapper.Map<UserDto>(updatedUser);
        }


        public async Task DeleteAsync(int id)
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user == null) 
                throw new NotFoundException("User not found");


            _uow.Users.Remove(user);
            await _uow.CompleteAsync();
        }
    }
}
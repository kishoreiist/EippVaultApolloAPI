using AutoMapper;
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


            // search (username, email, phone number)
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var keyword = query.Search.ToLower();

                usersQuery = usersQuery.Where(u =>
                    u.Username.ToLower().Contains(keyword) ||
                    u.Email.ToLower().Contains(keyword) ||
                    u.PhoneNumber.ToLower().Contains(keyword)
                );
            }

            // Role ID
            if (query.RoleId.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.RoleId == query.RoleId.Value);
            }

            //  Status
            if (query.Status.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.Status == query.Status.Value);
            }

            //MFA Enabled
            if (query.MfaEnabled.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.MfaEnabled == query.MfaEnabled.Value);
            }

            //  Email Verified
            if (query.EmailVerified.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.EmailVerified == query.EmailVerified.Value);
            }

            //  MFA Method
            if (query.MfaMethod.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.MfaMethod == query.MfaMethod);
            }

            //  Date Range
            if (query.FromDate.HasValue)
            {
                usersQuery = usersQuery.Where(g => g.CreatedAt >= query.FromDate.Value);
            }
            if (query.ToDate.HasValue)
            {
                usersQuery = usersQuery.Where(g => g.CreatedAt <= query.ToDate.Value);
            }

            //  TOTAL count BEFORE pagination
            var totalRecords = usersQuery.Count();

            // APPLY PAGINATION
            var pagedUsers = usersQuery
                .Skip(query.Offset)
                .Take(query.Limit)
                .ToList();

            // MAP TO DTO
            var userDtos = _mapper.Map<List<UserDto>>(pagedUsers);

            //  RETURN PAGED RESPONSE
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
                throw new ConflictException($"Username '{dto.Username}' already exists");


            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
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

            await _uow.Users.AddAsync(user);
            await _uow.CompleteAsync();
            return _mapper.Map<UserDto>(user);
        }


        public async Task<UserDto> UpdateAsync(UpdateUserDto dto)
        {
            var user = await _uow.Users.GetByIdAsync(dto.UserId);
            if (user == null) 
                throw new NotFoundException($"User with id {dto.UserId} not found");


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
            if (user == null) 
                throw new NotFoundException("User not found");


            _uow.Users.Remove(user);
            await _uow.CompleteAsync();
        }
    }
}
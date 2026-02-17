using AutoMapper;
using EVWebApi.DTOs;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;
using EVWebApi.Exceptions;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using IEmailSender = EVWebApi.Interfaces.Repositories.IEmailSender;

namespace EVWebApi.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IAuthService _authService;
        private readonly string _frontendRoot;
        private readonly IEmailSender _emailSender;
        private readonly string  _displayName;
        private readonly string _pdfviewer;
        private readonly IUserRepository _userRepo;
        public UserService(IUnitOfWork uow, IMapper mapper, IAuthService authService, IConfiguration config, IEmailSender emailSender, IUserRepository userRepo)
        {
            _uow = uow;
            _mapper = mapper;
            _authService = authService;
            _emailSender = emailSender;
            _frontendRoot = config["Frontend:BaseUrl"];
            _displayName = config["Email:DisplayName"];
            _pdfviewer = config["PDFViewFEUrl:BaseUrl"];
            _userRepo = userRepo;

        }


        public async Task<PagedResponse<UserDto>> GetAllAsync(UserQueryParameters query)
        {
            var usersQuery = _uow.Users.Query();

            if (!string.IsNullOrWhiteSpace(query.Username))
                usersQuery = usersQuery.Where(u => u.Username.Contains(query.Username));

            if (!string.IsNullOrWhiteSpace(query.FirstName))
                usersQuery = usersQuery.Where(u => u.FirstName.ToLower().Contains(query.FirstName.ToLower()));

            if (!string.IsNullOrWhiteSpace(query.LastName))
                usersQuery = usersQuery.Where(u => u.LastName.ToLower().Contains(query.LastName.ToLower()));

            if (!string.IsNullOrWhiteSpace(query.Email))
                usersQuery = usersQuery.Where(u => u.Email.ToLower().Contains(query.Email.ToLower()));

            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                usersQuery = usersQuery.Where(u => u.PhoneNumber.Contains(query.PhoneNumber));


            if (query.GroupId.HasValue)
                usersQuery = usersQuery.Where(u => u.UserGroup != null &&
                             u.UserGroup.Group.GroupId==query.GroupId.Value);

            //  TOTAL count BEFORE pagination
            var totalRecords = await usersQuery.CountAsync();
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
            // APPLY PAGINATION
            var pagedUsers = usersQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

        
        // MAP TO DTO
        var userDtos = _mapper.Map<List<UserDto>>(pagedUsers);
            return new PagedResponse<UserDto>
            {
                Data = userDtos,
                TotalRecords = totalRecords,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
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
            var normalizedEmail = EmailValidationHelper.Normalize(dto.Email);

            if (!EmailValidationHelper.IsValidEmail(normalizedEmail))
                throw new ValidationException("Invalid email address format");

            var exists = await _uow.Users.GetByUsernameAsync(dto.Username);
            if (exists != null)
                throw new ConflictException($"Username '{dto.Username}' already exists");


            var userexists = await _uow.Users.GetByEmailAsync(normalizedEmail);
            if (userexists != null)
            {
                if(userexists.Status == UserStatus.Locked)
                    throw new LockedException($"User with email - {normalizedEmail}' exists but is in locked state.");
            
                else
                    throw new ConflictException($"User mail '{normalizedEmail}' already exists");
            }

            var systemPassword = GenerateSystemPassword();

            var user = new User
            {
                Username = dto.Username,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(systemPassword, workFactor: 12),
                Email = normalizedEmail,
                MfaEnabled = dto.MfaEnabled,
                //Status = dto.Status ? UserStatus.active : UserStatus.inactive,
                Status = UserStatus.New, // New users,after paswrod reset becomes active
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailGroupId = dto.EmailGroupId,
                UserGroup = new UserGroup
                {
                    GroupId = dto.GroupId
                }
            };

            await _uow.Users.AddAsync(user);
            await _uow.CompleteAsync();

            var updatedUser = await _uow.Users.GetByIdAsync(user.UserId);
            var mappeduser = _mapper.Map<UserDto>(updatedUser);

            // Generate password reset token and send welcome email
            var token = _authService.GeneratePasswordResetJwtAsync(updatedUser);
            var resetUrl= string.Empty;

            if (mappeduser.UserType=="external")
            {
                 resetUrl = $"{_pdfviewer}reset_password?email={user.Email}&token={Uri.EscapeDataString(token)}";
            }
            else
            {
                 resetUrl = $"{_frontendRoot}reset_password?email={user.Email}&token={Uri.EscapeDataString(token)}";
            }

            var send= await _emailSender.SendAsync(
              ReplyTo:null,
              UserName: null,
               toEmail: user.Email,
                subject: $"Welcome to {_displayName}",
                htmlBody: $@"
                <div style='margin:0;padding:0;background-color:#f4f6f8;font-family:Arial,Helvetica,sans-serif;'>
                  <table width='100%' cellpadding='0' cellspacing='0'>
                    <tr>
                      <td align='center' style='padding:40px 16px;'>
                
                        <!-- Card -->
                        <table width='600' cellpadding='0' cellspacing='0'
                               style='background:#ffffff;border-radius:8px;
                                      box-shadow:0 4px 12px rgba(0,0,0,0.08);'>
                
                          <!-- Body -->
                          <tr>
                            <td style='padding:32px;color:#111827;font-size:15px;line-height:1.6;'>
                
                              <p>Dear User,</p>
                
                              <p>
                                Welcome to <strong>{_displayName}</strong>. Your account has been successfully created.
                              </p>
                
                              <ul style='padding-left:18px;'>
                                <li><strong>Username:</strong> {user.Username}</li>
                                <li><strong>Email:</strong> {user.Email}</li>
                              </ul>
                
                              <p>
                                For security reasons, you must set your password before signing in.
                              </p>
                
                              <!-- Button -->
                              <table width='100%' cellpadding='0' cellspacing='0' style='margin:32px 0;'>
                                <tr>
                                  <td align='center'>
                                    <a href='{resetUrl}' target='_blank'
                                       style='background:#2563eb;color:#ffffff;
                                              text-decoration:none;padding:14px 28px;
                                              border-radius:6px;font-size:16px;
                                              display:inline-block;'>
                                      Set Your Password
                                    </a>
                                  </td>
                                </tr>
                              </table>
                
                              <p style='font-size:13px;color:#6b7280;'>
                                This link will expire in <strong>30 minutes</strong>.
                              </p>
                
                              <hr style='border:none;border-top:1px solid #e5e7eb;margin:24px 0;' />
                
                              <p style='font-size:13px;color:#6b7280;'>
                                <i>If you did not request this account, please ignore this email
                                or contact our support team immediately.</i>
                              </p>
         
                              <p style='margin-top:24px;'>
                                Regards,<br/>
                                <strong>{_displayName} Team</strong>
                              </p>
                
                            </td>
                          </tr>
                        </table>
                
                      </td>
                    </tr>
                  </table>
                </div>
                "

            );


            //return _mapper.Map<UserDto>(updatedUser);
            if(!send)
            {
                throw new Exception("User created but failed to send welcome email.");
            }
            return mappeduser;
        }


        public async Task<UserDto> UpdateAsync(UpdateUserDto dto)
        {
            var user = await _userRepo.GetByIdAsync(dto.UserId);
            if (user == null) 
                throw new NotFoundException($"User with id {dto.UserId} not found");


            if (!string.IsNullOrWhiteSpace(dto.Username)) user.Username = dto.Username;
            if (!string.IsNullOrWhiteSpace(dto.FirstName)) user.FirstName = dto.FirstName;
            if (!string.IsNullOrWhiteSpace(dto.LastName)) user.LastName = dto.LastName;

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var normalizedEmail = EmailValidationHelper.Normalize(dto.Email);
                if (!EmailValidationHelper.IsValidEmail(normalizedEmail))
                    throw new ValidationException("Invalid email address"); 
                user.Email = normalizedEmail; 
            
            }

            if (dto.MfaEnabled.HasValue) user.MfaEnabled = dto.MfaEnabled.Value;
            if (dto.MfaMethod.HasValue) user.MfaMethod = dto.MfaMethod;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;
            //if (dto.EmailVerified.HasValue) user.EmailVerified = dto.EmailVerified.Value;
            if (dto.Status.HasValue) user.Status = dto.Status.Value;
            



            if (dto.GroupId != 0)
            {
                if (user.UserGroup == null)
                {
                    user.UserGroup = new UserGroup
                    {
                        UserId = user.UserId,
                        GroupId = dto.GroupId
                    };
                }
                else
                {
                    user.UserGroup.GroupId = dto.GroupId;
                }
            }
            if(dto.EmailGroupId.HasValue) user.EmailGroupId = dto.EmailGroupId;
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

            _uow.Users.SoftDelete(user);
           
            //_uow.Users.Remove(user);
            await _uow.CompleteAsync();
        }

        public async Task<List<EmailGroupUserDto>> GetUserByEmailGroupAsync(int groupid)
        {
            var users=await _uow.Groups.GetUsersByEmailGroupIdAsync(groupid);
            if (!users.Any())
                throw new Exception("No users found in this group");
            return users;
        }

        //hlper method to generate random password
        private static string GenerateSystemPassword()
        {
            return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64)
            );
        }
    }
}
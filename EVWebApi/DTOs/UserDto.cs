using EVWebApi.Models;

namespace EVWebApi.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public int RoleId { get; set; }
        public required string RoleName { get; set; }
        public UserStatus Status { get; set; }
        public bool MfaEnabled { get; set; }
        public MfaMethod? MfaMethod { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime? LastMfaVerifiedAt { get; set; }
    }
}

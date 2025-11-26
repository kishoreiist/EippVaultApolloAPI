using EVWebApi.Models;

namespace EVWebApi.DTOs.User
{
    public class UserDto
    {
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }

        public int RoleId { get; set; }
        public  string? RoleName { get; set; }

        public List<int>? GroupIds { get; set; }
        public List<string>? GroupNames { get; set; }

        public bool Status { get; set; }


        public bool MfaEnabled { get; set; }
        public string? PhoneNumber { get; set; }

    }
}

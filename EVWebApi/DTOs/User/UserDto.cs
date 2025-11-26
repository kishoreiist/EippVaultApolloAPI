using EVWebApi.Models;

namespace EVWebApi.DTOs.User
{
    public class UserDto
    {
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }




        public int GroupId { get; set; }
        public string GroupName { get; set; }

        public bool Status { get; set; }


        public bool MfaEnabled { get; set; }
        public string? PhoneNumber { get; set; }

    }
}

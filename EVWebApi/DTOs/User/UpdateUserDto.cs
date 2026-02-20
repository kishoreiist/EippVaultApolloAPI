using EVWebApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EVWebApi.DTOs.User
{
    public class UpdateUserDto
    {
        [Required]
        public int UserId { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }

        public int GroupId { get; set; }

        public UserStatus? Status { get; set; }

        public int? EmailGroupId { get; set; }
        public bool? MfaEnabled { get; set; }
        public MfaMethod? MfaMethod { get; set; }
        public string? PhoneNumber { get; set; }
        //public bool? EmailVerified { get; set; }

    }
}

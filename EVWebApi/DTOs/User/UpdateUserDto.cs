using EVWebApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EVWebApi.DTOs.User
{
    public class UpdateUserDto
    {
        [Required]
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public int? RoleId { get; set; }
        public List<int>? GroupIds { get; set; }

        public bool Status { get; set; }


        public bool? MfaEnabled { get; set; }
        public MfaMethod? MfaMethod { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? EmailVerified { get; set; }

    }
}

using System.ComponentModel.DataAnnotations;

namespace EVWebApi.DTOs.User
{
    public class CreateUserDto
    {
        [Required]
        public required string Username { get; set; }


        [Required]
        [EmailAddress]
        public required string Email { get; set; }


        [Required]
        public required string Password { get; set; }


        [Required]
        public int RoleId { get; set; }

        public List<int>? GroupIds { get; set; } = new();

        public bool Status { get; set; } = true;
        public bool MfaEnabled { get; set; }
        public string? PhoneNumber { get; set; }
    }
}

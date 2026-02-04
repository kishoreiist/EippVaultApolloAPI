using System.ComponentModel.DataAnnotations;

namespace EVWebApi.DTOs.User
{
    public class CreateUserDto
    {
        [Required]
        public required string Username { get; set; }
        [Required]
        public required string FirstName { get; set; }     
        [Required]
        public required string LastName { get; set; }
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }

        public int ? EmailGroupId { get; set; }
        public int GroupId { get; set; } = new();

        public bool Status { get; set; } = true;
        public bool MfaEnabled { get; set; }
        public string? PhoneNumber { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace EVWebApi.DTOs.Role
{
    public class CreateRoleDto
    {

        [Required]
        public required string RoleName { get; set; }
        public Dictionary<string, bool>? Permissions { get; set; }
    }
}

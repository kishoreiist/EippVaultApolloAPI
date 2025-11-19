using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace EVWebApi.DTOs
{
    public class UpdateRoleDto
    {
        [Required]
        public required string RoleName { get; set; }
        [Required]
        public required int RoleId { get; set; }
        public JsonElement? Permissions { get; set; }
    }
}

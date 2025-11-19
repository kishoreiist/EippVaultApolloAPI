
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace EVWebApi.DTOs
{
    public class CreateRoleDto
    {

        [Required]
        public required string RoleName { get; set; }
        public JsonElement? Permissions { get; set; }
    }
}

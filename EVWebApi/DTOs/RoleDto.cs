using System.Text.Json;

namespace EVWebApi.DTOs
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public required string RoleName { get; set; }
        public JsonElement? Permissions { get; set; }
    }
}

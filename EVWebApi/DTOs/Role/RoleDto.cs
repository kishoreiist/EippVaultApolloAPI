using System.Text.Json;

namespace EVWebApi.DTOs.Role
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public required string RoleName { get; set; }
        public Dictionary<string, bool>? Permissions { get; set; }
    }
}

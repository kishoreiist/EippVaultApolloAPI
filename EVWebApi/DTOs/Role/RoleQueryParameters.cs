using EVWebApi.DTOs.Pagination;

namespace EVWebApi.DTOs.Role
{
    public class RoleQueryParameters : QueryParameters
    {
        //public Dictionary<string, bool>? Permissions { get; set; }
        public string? PermissionKey { get; set; }
    }

}

using EVWebApi.DTOs.Cabinet;
using EVWebApi.DTOs.Group;
using EVWebApi.Models;

namespace EVWebApi.DTOs.User
{
    public class UserDto
    {
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; } 

        public int GroupId { get; set; }
        //public GroupDescriptionDTO? Description { get; set; }
        public string GroupName { get; set; }
        public string UserType { get; set; }
        public List<ListDto> AccessList { get; set; }
        public List<ListDto> CabinetsList { get; set; }
        public string Status { get; set; }
        public int? EmailGroupId { get; set; }

        public bool MfaEnabled { get; set; }
        public string? PhoneNumber { get; set; }

    }
}

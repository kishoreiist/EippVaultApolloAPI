using EVWebApi.DTOs.Pagination;
using EVWebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.DTOs.User
{
    public class UserQueryParameters : QueryParameters
    {

        [FromQuery(Name = "user_name")]
        public string? Username { get; set; }

        [FromQuery(Name = "email")]
        public string? Email { get; set; }

        [FromQuery(Name = "phone_number")]
        public string? PhoneNumber { get; set; }

        [FromQuery(Name = "role_name")]
        public string? RoleName { get; set; }

        [FromQuery(Name = "group_name")]
        public string? GroupName { get; set; }

        //[FromQuery(Name = "status")]
        //public UserStatus? Status { get; set; }


    }

}

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
        [FromQuery(Name = "first_name")]
        public string? FirstName { get; set; }

        [FromQuery(Name = "last_name")]
        public string? LastName { get; set; }

        [FromQuery(Name = "group_id")]
        public int? GroupId { get; set; }

        //[FromQuery(Name = "status")]
        //public UserStatus? Status { get; set; }


    }

}

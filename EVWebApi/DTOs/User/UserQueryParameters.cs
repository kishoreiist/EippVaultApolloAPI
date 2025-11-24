using EVWebApi.DTOs.Pagination;
using EVWebApi.Models;

namespace EVWebApi.DTOs.User
{
    public class UserQueryParameters : QueryParameters
    {
        public int? RoleId { get; set; }

        public UserStatus? Status { get; set; }

        public bool? MfaEnabled { get; set; }

        public bool? EmailVerified { get; set; }

        public MfaMethod? MfaMethod { get; set; }


    }

}

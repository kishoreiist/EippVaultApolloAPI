using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.Models
{
    public enum UserStatus { active, inactive }
    public enum MfaMethod { email, sms, authenticator }


    public class User
    {
        public int UserId { get; set; }

        [Column("username")]
        public required string Username { get; set; }
        [Column("email")]
        public required string Email { get; set; }
        [Column("password_hash")]
        public required string PasswordHash { get; set; }
        [Column("role_id")]
        public int RoleId { get; set; }
       
        public Role? Role { get; set; }

        [Column("status")]
        public UserStatus Status { get; set; } = UserStatus.active;
        [Column("mfa_enabled")]
        public bool MfaEnabled { get; set; } = false;
        [Column("mfa_method")]
        public MfaMethod? MfaMethod { get; set; }
        [Column("phone_number")]
        public string? PhoneNumber { get; set; }
        [Column("email_verified")]
        public bool EmailVerified { get; set; } = false;
        [Column("last_mfa_verified_at")]
        public DateTime? LastMfaVerifiedAt { get; set; }


        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }


        // Many-to-many
        public required ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
        public ICollection<UserMfaToken> MfaTokens { get; set; }
    }
}
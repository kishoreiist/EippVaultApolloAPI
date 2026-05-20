using EVWebApi.Helpers;
using EVWebApi.Models.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static QRCoder.PayloadGenerator;

namespace EVWebApi.Models
{
    public enum UserStatus { New, Active, Disabled, Locked,Deleted }
    public enum MfaMethod { email, sms, authenticator }


    public class User
    {
        public int UserId { get; set; }

        [Column("username")]
        public required string Username { get; set; }

        [Column("first_name")]
        public required string FirstName { get; set; }
        [Column("last_name")]
        public required string LastName { get; set; }
        [Column("password_hash")]
        public required string PasswordHash { get; set; }

        [Column("status")]
        public UserStatus Status { get; set; }
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

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("password_changed_at")]
        public DateTime? PasswordChangedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        //email entity level normalization
        private string _email = string.Empty;
        [Column("email")]
        public string Email
        {
            get => _email;
            set => _email = EmailValidationHelper.Normalize(value);
        }

        [Column("email_group_id")]
        public int? EmailGroupId { get; set; }

        [ForeignKey(nameof(EmailGroupId))]
        public EmailGroup? EmailGroup { get; set; }

        // junction table
        public UserGroup? UserGroup { get; set; }
        public ICollection<UserMfaToken> MfaTokens { get; set; }
        public ICollection<AccountLockAudit> LockAudits { get; set; }
    }
}
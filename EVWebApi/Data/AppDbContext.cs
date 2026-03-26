using EVWebApi.Helpers;
using EVWebApi.Models;
using EVWebApi.Models.Security;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace EVWebApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets
        public DbSet<User> Users { get; set; }

        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }

        public DbSet<UserAuthenticator> UserAuthenticators { get; set; }
        public DbSet<UserMfaToken> UserMfaTokens { get; set; }
        public DbSet<Notes> Notes { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocDownloadLink> DocumentLink { get; set; }
        public DbSet<AccessRights> AccessRights { get; set; }
        public DbSet<DocumentTypes> DocumentTypes { get; set; }
        public DbSet<Cabinet> Cabinets { get; set; }
        public DbSet<Metadata> Metadata { get; set; }
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EmailGroup> EmailGroups { get; set; }

        public DbSet<GroupAccessRight> GroupAccessRights { get; set; }
        public DbSet<GroupCabinet> GroupCabinets { get; set; }
        public DbSet<CabinetGroupingRule> CabinetGroupingRules { get; set; }


        public DbSet<AccountLockAudit> LockAudit { get; set; }
        public DbSet<IpSecurityState> IpSecurity { get; set; }
        public DbSet<UserFailureRecord> UserFailureRecords { get; set; }
        public DbSet<IpPermanentLockLog> IpPermanentLockLogs { get; set; }
        public DbSet<IpFailureRecord> IpFailureRecords { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserPasswordHistory> UserPasswords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
            base.OnModelCreating(modelBuilder);

            // -------------------
            // Table Names
            // -------------------
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Group>().ToTable("groups");
            modelBuilder.Entity<Document>().ToTable("documents");
            modelBuilder.Entity<Metadata>().ToTable("metadata");
            modelBuilder.Entity<UserGroup>().ToTable("user_groups");
            modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
            modelBuilder.Entity<Cabinet>().ToTable("cabinets");
            modelBuilder.Entity<UserAuthenticator>().ToTable("user_authenticator");
            modelBuilder.Entity<UserMfaToken>().ToTable("user_mfa_tokens");
            modelBuilder.Entity<Notes>().ToTable("notes");
            modelBuilder.Entity<AccessRights>().ToTable("accessrights");
            modelBuilder.Entity<DocumentTypes>().ToTable("document_types");
            modelBuilder.Entity<CabinetGroupingRule>().ToTable("cabinet_grouping_rules");
            modelBuilder.Entity<EmailGroup>().ToTable("email_group");
            modelBuilder.Entity<GroupCabinet>().ToTable("group_cabinets");
            modelBuilder.Entity<GroupAccessRight>().ToTable("group_accessrights");
            modelBuilder.Entity<DocDownloadLink>().ToTable("doc_download_link");

            modelBuilder.Entity<AccountLockAudit>().ToTable("account_lock_audit");
            modelBuilder.Entity<IpSecurityState>().ToTable("ip_security_state");
            modelBuilder.Entity<UserFailureRecord>().ToTable("user_failure_record");
            modelBuilder.Entity<IpPermanentLockLog>().ToTable("ip_permanent_lock_log");
            modelBuilder.Entity<IpFailureRecord>().ToTable("ip_failure_record");
            modelBuilder.Entity<UserSession>().ToTable("user_sessions");
            modelBuilder.Entity<UserPasswordHistory>().ToTable("user_password_history");


            // -------------------
            // Primary Keys
            // -------------------
            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Group>().HasKey(g => g.GroupId);
            modelBuilder.Entity<AccessRights>().HasKey(a => a.Id);
            modelBuilder.Entity<Cabinet>().HasKey(u => u.CabinetId);
            modelBuilder.Entity<DocumentTypes>().HasKey(d => d.Id);
            modelBuilder.Entity<CabinetGroupingRule>().HasKey(d => d.Id);
            modelBuilder.Entity<UserSession>().HasKey(d => d.Id);
            modelBuilder.Entity<UserPasswordHistory>().HasKey(d => d.Id);

            modelBuilder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.GroupId });

            modelBuilder.Entity<UserAuthenticator>()
                .HasKey(a => a.AuthId);

            modelBuilder.Entity<UserMfaToken>()
                .HasKey(t => t.TokenId);
            modelBuilder.Entity<Notes>().HasKey(n => n.NoteId);

            modelBuilder.Entity<GroupAccessRight>().HasKey(a => new { a.GroupId, a.AccessId });
            modelBuilder.Entity<GroupCabinet>().HasKey(c => new { c.GroupId, c.CabinetId });
            // -------------------
            // User Relationships
            // -------------------
            modelBuilder.Entity<GroupAccessRight>()
                .HasOne(e => e.Group)
                .WithMany(g => g.GroupAccessRights)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupAccessRight>()
                  .HasOne(e => e.AccessRight)
                  .WithMany(a => a.GroupAccessRights)
                  .HasForeignKey(e => e.AccessId)
                  .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<GroupCabinet>()
                 .HasOne(e => e.Group)
                 .WithMany(g => g.GroupCabinets)
                 .HasForeignKey(e => e.GroupId)
                 .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<GroupCabinet>()
                  .HasOne(e => e.Cabinet)
                  .WithMany(c => c.GroupCabinets)
                  .HasForeignKey(e => e.CabinetId)
                  .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<UserGroup>()
            .HasKey(ug => ug.UserId); // Primary Key is UserId (NOT composite)

            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.User)
                .WithOne(u => u.UserGroup)
                .HasForeignKey<UserGroup>(ug => ug.UserId);

            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.GroupId);


            modelBuilder.Entity<DocDownloadLink>()
            .HasIndex(x => new { x.DocumentId, x.UserId })
            .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.UserId)
                .HasColumnName("user_id");

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .HasColumnName("password_hash");

            // One user can have only one UserGroup
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserGroup)
                .WithOne(ug => ug.User)
                .HasForeignKey<UserGroup>(ug => ug.UserId);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.DocumentType)
                .WithMany(dt => dt.Documents)
                .HasForeignKey(d => d.DocumentTypeId)
                .HasConstraintName("documents_doc_type_fk")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AccountLockAudit>()
                .HasOne(x => x.User)
                .WithMany(u => u.LockAudits)
                .HasForeignKey(x => x.UserId);

            // -------------------
            // Auth Entities
            // -------------------
            modelBuilder.Entity<UserAuthenticator>()
                .Property(a => a.AuthId)
                .HasColumnName("auth_id");

            modelBuilder.Entity<UserAuthenticator>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId);

            modelBuilder.Entity<UserMfaToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId);

            // -------------------
            // Enum → string
            // -------------------
            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasConversion<string>();

            modelBuilder.Entity<User>()
                .Property(u => u.MfaMethod)
                .HasConversion<string>();



            //modelBuilder.Entity<Group>()
            //   .Property(r => r.Description)
            //   .HasColumnType("jsonb");

            // -------------------
            // Timestamps
            // -------------------


            modelBuilder.Entity<User>().Property(u => u.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<User>().Property(u => u.UpdatedAt).HasColumnName("updated_at");

            modelBuilder.Entity<Group>().Property(g => g.CreatedAt).HasColumnName("created_at");

            modelBuilder.Entity<UserMfaToken>().Property(e => e.TokenId).HasColumnName("token_id");
            modelBuilder.Entity<UserMfaToken>().Property(e => e.Token).HasColumnName("mfa_token");
            modelBuilder.Entity<UserMfaToken>().Property(e => e.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<UserMfaToken>().Property(e => e.ExpiresAt).HasColumnName("expires_at");
            modelBuilder.Entity<UserMfaToken>().Property(e => e.UserId).HasColumnName("user_id");
            modelBuilder.Entity<UserMfaToken>().Property(e => e.Used).HasColumnName("used");
            modelBuilder.Entity<User>().Property(u => u.PasswordChangedAt).HasColumnName("password_changed_at");


            modelBuilder.Entity<User>()
                .Property(u => u.MfaEnabled).HasColumnName("mfa_enabled");
            modelBuilder.Entity<User>()
                .Property(u => u.MfaMethod).HasColumnName("mfa_method");
            modelBuilder.Entity<User>()
                .Property(u => u.EmailVerified).HasColumnName("email_verified");

            modelBuilder.Entity<UserMfaToken>()
                .Property(e => e.ExpiresAt)
                .HasColumnType("timestamp with time zone");

            
            modelBuilder.Entity<UserMfaToken>()
                .HasOne(t => t.User)
                .WithMany(u => u.MfaTokens) // Explicit collection in User model
                .HasForeignKey(t => t.UserId);

            // -------------------
            // Indexes
            // -------------------
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Group>().HasIndex(g => g.GroupName).IsUnique();
            modelBuilder.Entity<UserMfaToken>().HasIndex(t => new { t.UserId, t.Token });

            modelBuilder.Entity<UserFailureRecord>()
                .HasIndex(x => new { x.UserId, x.Endpoint })
                .IsUnique()
                .HasFilter("\"is_active\" = true");

            modelBuilder.Entity<IpSecurityState>()
                .HasIndex(x => x.IpAddress)
                .IsUnique();



            modelBuilder.Entity<UserAuthenticator>(entity =>
            {
                entity.ToTable("user_authenticator");

                entity.HasKey(e => e.AuthId);
                entity.Property(e => e.AuthId).HasColumnName("auth_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.SecretKey).HasColumnName("secret_key");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.Enabled).HasColumnName("enabled");

                entity.HasOne(e => e.User)
                 .WithMany() 
                 .HasForeignKey(e => e.UserId);
            });


            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.ExpiresAt);
               

                entity.Property(x => x.JwtId)
                    .IsRequired()
                    .HasMaxLength(100);
            });

        }
    }
}

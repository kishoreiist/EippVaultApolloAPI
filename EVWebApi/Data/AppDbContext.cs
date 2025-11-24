using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }

        public DbSet<UserAuthenticator> UserAuthenticators { get; set; }
        public DbSet<UserMfaToken> UserMfaTokens { get; set; }

        public DbSet<Document> Documents { get; set; }
        public DbSet<Cabinet> Cabinets { get; set; }
        public DbSet<Metadata> Metadata { get; set; }
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
            base.OnModelCreating(modelBuilder);

            // -------------------
            // Table Names
            // -------------------
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Role>().ToTable("roles");
            modelBuilder.Entity<Group>().ToTable("groups");
            modelBuilder.Entity<Document>().ToTable("documents");
            modelBuilder.Entity<Metadata>().ToTable("metadata");
            modelBuilder.Entity<UserGroup>().ToTable("user_groups");
            modelBuilder.Entity<AuditLog>().ToTable("audit_logs");

            modelBuilder.Entity<UserAuthenticator>().ToTable("user_authenticator");
            modelBuilder.Entity<UserMfaToken>().ToTable("user_mfa_tokens");

            // -------------------
            // Primary Keys
            // -------------------
            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Role>().HasKey(r => r.RoleId);
            modelBuilder.Entity<Group>().HasKey(g => g.GroupId);

            modelBuilder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.GroupId });

            modelBuilder.Entity<UserAuthenticator>()
                .HasKey(a => a.AuthId);

            modelBuilder.Entity<UserMfaToken>()
                .HasKey(t => t.TokenId);

            // -------------------
            // User Relationships
            // -------------------
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - UserGroups
            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.User)
                .WithMany(u => u.UserGroups)
                .HasForeignKey(ug => ug.UserId);

            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.GroupId);

            modelBuilder.Entity<User>()
                .Property(u => u.UserId)
                .HasColumnName("user_id");

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .HasColumnName("password_hash");

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

            // -------------------
            // JSONB Permissions
            // -------------------
            modelBuilder.Entity<Role>()
                .Property(r => r.Permissions)
                .HasColumnType("jsonb");

            // -------------------
            // Timestamps
            // -------------------
            modelBuilder.Entity<Role>().Property(r => r.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<Role>().Property(r => r.UpdatedAt).HasColumnName("updated_at");

            modelBuilder.Entity<User>().Property(u => u.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<User>().Property(u => u.UpdatedAt).HasColumnName("updated_at");

            modelBuilder.Entity<Group>().Property(g => g.CreatedAt).HasColumnName("created_at");

            modelBuilder.Entity<UserMfaToken>().Property(e => e.TokenId).HasColumnName("token_id");
            modelBuilder.Entity<UserMfaToken>().Property(e => e.Token).HasColumnName("mfa_token");
            modelBuilder.Entity<UserMfaToken>().Property(e => e.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<UserMfaToken>().Property(e => e.ExpiresAt).HasColumnName("expires_at");
            modelBuilder.Entity<UserMfaToken>().Property(e => e.UserId).HasColumnName("user_id");
            modelBuilder.Entity<UserMfaToken>().Property(e => e.Used).HasColumnName("used");


            modelBuilder.Entity<User>()
                .Property(u => u.MfaEnabled).HasColumnName("mfa_enabled");
            modelBuilder.Entity<User>()
                .Property(u => u.MfaMethod).HasColumnName("mfa_method");


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
            modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
            modelBuilder.Entity<Group>().HasIndex(g => g.GroupName).IsUnique();
            modelBuilder.Entity<UserMfaToken>().HasIndex(t => new { t.UserId, t.Token });




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
           
        }
    }
}

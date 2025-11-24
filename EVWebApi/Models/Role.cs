using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace EVWebApi.Models
{
        public class Role
        {
             [Column("role_id")]
             public int RoleId { get; set; }
             [Column("role_name")]
             public required string RoleName { get; set; }
             [Column("permissions", TypeName = "jsonb")]
             public Dictionary<string, bool>? Permissions { get; set; }

            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
       
        //one-to-many
            public ICollection<User>? Users { get; set; }
        }
    }


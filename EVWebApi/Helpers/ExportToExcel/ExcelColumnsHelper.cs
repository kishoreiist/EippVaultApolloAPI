using EVWebApi.DTOs.Audit;
using EVWebApi.DTOs.Security;
using EVWebApi.Models;
using EVWebApi.Models.Security;

namespace EVWebApi.Helpers.ExportToExcel
{
    public static class ExcelColumnsHelper
    {
        // ================= USERS =================
        public static readonly Dictionary<string, Func<User, object?>> UserColumns = new()
        {
            ["Username"] = u => u.Username,
            ["FirstName"] = u => u.FirstName,
            ["LastName"] = u => u.LastName,
            ["Email"] = u => u.Email,
            ["Status"] = u => u.Status,
            ["GroupName"] = u => u.UserGroup?.Group?.GroupName,
            ["PhoneNumber"] = u => u.PhoneNumber,
            ["CreatedAt"] = u => u.CreatedAt
        };

        public static object? GetUserColumnValue(User user, string column)
        {
            return UserColumns.TryGetValue(column, out var selector)
                ? selector(user)
                : null;
        }


        // ================= GROUPS =================
        public static readonly Dictionary<string, Func<Group, object?>> GroupColumns = new()
        {
            ["GroupName"] = g => g.GroupName,
            ["UserType"] = g => g.UserType,
            ["AccessList"] = g => g.GroupAccessRights != null
                ? string.Join(", ", g.GroupAccessRights
                    .Where(gr => gr.AccessRight != null)
                    .Select(gr => gr.AccessRight.AccessName))
                : string.Empty,
            ["CabinetList"] = g => g.GroupCabinets != null
                ? string.Join(", ", g.GroupCabinets
                    .Select(c => c.Cabinet.CabinetName))
                : string.Empty,
            ["CreatedAt"] = g => g.CreatedAt

        };

        public static object? GetGroupColumnValue(Group group, string column)
        {
            return GroupColumns.TryGetValue(column, out var selector)
                ? selector(group)
                : null;
        }


        // ================= LOGS =================
        public static readonly Dictionary<string, Func<AuditLogDTO, object?>> LogColumns = new()
        {
            ["UserName"] = l => l.UserName,
            ["Action"] = l => l.Action,
            ["IP Address"] = l => l.IpAddress,
            ["Module"] = l => l.Module,
            ["Timestamp"] = l => l.Timestamp,
            ["Message"] = l => l.Details
        };

        public static object? GetLogColumnValue(AuditLogDTO log, string column)
        {
            return LogColumns.TryGetValue(column, out var selector)
                ? selector(log)
                : null;
        }


        //======================LCOKED USERS======================
        public static readonly Dictionary<string, Func<AccountLockAudit, object?>> LockedUserColumns = new()
        {
            ["Name"]=l=> $"{l.User?.FirstName} {l.User?.LastName}",
            ["Reason"] = l => l.Reason,
            ["LockType"] = l => l.LockType,
            ["LockedTime"] = l => l.LockedAt
        };

        public static object? GetLockedUserColumnValue(AccountLockAudit locked, string column)
        {
            return LockedUserColumns.TryGetValue(column, out var selector)
                ? selector(locked)
                : null;
        }

        //======================BLACKLISTED IPS======================
        public static readonly Dictionary<string, Func<IpSecurityState, object?>> IPStatusColumns = new()
        {
            ["IPAddress"] = l => l.IpAddress,
            ["Status"] = l => l.Status,
            ["DailyFailures"] = l => l.IPDailyFailures,
            ["WeeklyFailures"] = l => l.IPWeeklyFailures,
            ["BlackListedTime"] = l => l.BlacklistedAt,
            ["LastActivityTime"] = l => l.LastActivityAt
        };

        public static object? GetIPStatusColumnValue(IpSecurityState ip, string column)
        {
            return IPStatusColumns.TryGetValue(column, out var selector)
                ? selector(ip)
                : null;
        }

    }
}
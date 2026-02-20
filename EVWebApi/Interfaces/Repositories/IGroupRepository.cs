using EVWebApi.DTOs.Group;
using EVWebApi.Models;

namespace EVWebApi.Interfaces.Repositories
{
    public interface IGroupRepository : IGenericRepository<Group>
    {
        Task<Group> GetByGroupnameAsync(string groupname);
        Task<List<GroupListDto>> GetGroupsForDropdownAsync();
        IQueryable<Group> Query();
        Task<bool> GetUsersAsync(int id);

        //--------------email grp--------

        Task<EmailGroup> GetByEmailGroupnameAsync(string group_name);
        Task<List<EmailGroup>> GeAllEmailGroupsAsync();
        Task<EmailGroup> AddEmailGroupAsync(EmailGroup group);
        Task<EmailGroup> GetEmailGroupByIdAsync(int id);
        void UpdateEmailGroupAsync(EmailGroup group);
        void RemoveEmailGroupAsync(EmailGroup group);

        Task<List<EmailGroupUserDto>> GetUsersByEmailGroupIdAsync(int emailGroupId);
    }
}

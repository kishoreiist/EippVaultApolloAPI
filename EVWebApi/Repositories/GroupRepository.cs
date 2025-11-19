using EVWebApi.Data;
using EVWebApi.Interfaces;
using EVWebApi.Models;


namespace EVWebApi.Repositories
{
    public class GroupRepository : GenericRepository<Group>, IGroupRepository
    {
        public GroupRepository(AppDbContext context) : base(context) 
        { 
        }
    }
}

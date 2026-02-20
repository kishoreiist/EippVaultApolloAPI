
namespace EVWebApi.DTOs.Group
{
    public class ListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class EmailGroupUserDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }

    }
    public class GroupListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UserType { get; set; }
    }
}
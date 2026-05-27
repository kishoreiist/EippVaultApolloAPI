namespace EVWebApi.DTOs.Group
{
    public class CreateGroupDto
    {
        public string GroupName { get; set; } = null!;
        public string UserType { get; set; }
        public string? Region { get; set; }
        public List<int>? AccessList { get; set; }
        public List<int>? CabinetsList { get; set; }
    }

    public class CreateEmailGroupDto
     {
        public string GroupName { get; set; }
        public Boolean IsExternal { get; set; }

    }

}

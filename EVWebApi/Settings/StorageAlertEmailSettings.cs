namespace EVWebApi.Settings
{
    public class StorageAlertEmailSettings
    {
        public string To { get; set; }
        public List<string> Cc { get; set; }
        public string ReplyTo { get; set; }
        public string UserName { get; set; }
    }
}

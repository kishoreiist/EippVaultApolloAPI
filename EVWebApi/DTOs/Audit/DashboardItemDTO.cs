namespace EVWebApi.DTOs.Audit
{


    public class DashboardDTO
    {
        public KpiData kpis { get; set; }
        public List<UploadPercentageItem> upload_percentage { get; set; }
        public List<CabinetDistributionItem> cabinet_distribution { get; set; }
        public List<double> user_activity_percentage { get; set; }
    }

    public class KpiData
    {
        public int Cabinets { get; set; }
        public int total_records { get; set; }
        public int uploaded_files { get; set; }
        public int usertotal_records { get; set; }
        public int useruploaded_files { get; set; }
    }

    public class UploadPercentageItem
    {
        public string day { get; set; }
        public int count { get; set; }
        public double percentage { get; set; }
    }

    public class CabinetDistributionItem
    {
        public string cabinet { get; set; }
        public double percentage { get; set; }
    }
}

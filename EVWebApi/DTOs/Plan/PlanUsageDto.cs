namespace EVWebApi.DTOs.Plan
{
    public class PlanUsageDto
    {
        public decimal TotalGb { get; set; }
        public decimal UsedGb { get; set; }
        public decimal RemainingGb { get; set; }
        public decimal RemaianingBytes { get; set; }
        public double UsagePercentage { get; set; }
    }
}

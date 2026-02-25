namespace ProCosmeticsSystem.Application.DTOs.Reports;

public class DeliveryReportDto
{
    public int TotalDeliveries { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingCount { get; set; }
    public decimal SuccessRate { get; set; }
    public double? AverageDeliveryTimeHours { get; set; }
}

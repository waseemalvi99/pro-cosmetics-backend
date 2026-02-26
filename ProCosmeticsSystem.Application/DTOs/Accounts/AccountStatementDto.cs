namespace ProCosmeticsSystem.Application.DTOs.Accounts;

public class AccountStatementDto
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<AccountStatementLineDto> Lines { get; set; } = [];
}

public class AccountStatementLineDto
{
    public int Id { get; set; }
    public DateTime EntryDate { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
}

public class AgingReportDto
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime AsOfDate { get; set; }
    public decimal TotalCurrent { get; set; }
    public decimal Total1To30 { get; set; }
    public decimal Total31To60 { get; set; }
    public decimal Total61To90 { get; set; }
    public decimal TotalOver90 { get; set; }
    public decimal GrandTotal { get; set; }
    public List<AgingDetailDto> Details { get; set; } = [];
}

public class AgingDetailDto
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }
    public decimal Total { get; set; }
}

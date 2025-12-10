public sealed class PartnerAgingSummaryDto {
    public int PartnerId { get; set; }
    public string Title { get; set; } = null!;
    public decimal Total { get; set; }
    public Dictionary<string, decimal> Buckets { get; set; } = new();
    public decimal? CreditLimitTry { get; set; }
}

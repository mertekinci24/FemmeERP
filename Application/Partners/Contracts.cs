using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace InventoryERP.Application.Partners;

public sealed record PartnerRowDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string TaxNo { get; init; } = string.Empty;
    public string? NationalId { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
    public decimal TotalDebit { get; init; }
    public decimal TotalCredit { get; init; }
    public decimal? BalanceTry { get; init; }
    public decimal? CreditLimitTry { get; init; }
    public DateTime? LastMovementDate { get; init; }
    public string? LastDocNumber { get; init; }
    public string? LastDocType { get; init; }
    public decimal? LastDebit { get; init; }
    public decimal? LastCredit { get; init; }
    public decimal? LastBalanceAfter { get; init; }
    public string TaxId => TaxNo;
    public string? LastMovementSummary => LastMovementDate is null
        ? null
        : $"{LastMovementDate:dd.MM.yyyy} - {LastDocType} {LastDocNumber}";
}
public sealed record PartnerDetailDto(int Id, string Title, string Role, string TaxNo, decimal? BalanceTry, decimal? CreditLimitTry);
public sealed record PartnerStatementRowDto(System.DateTime Date, string DocType, string DocNumber, string Description, decimal Debit, decimal Credit, decimal AmountTry, decimal BalanceAfter, string Direction);
public sealed record AgingBucketDto(string Bucket, decimal AmountTry);
public sealed record StatementDto(IReadOnlyList<PartnerStatementRowDto> Rows, decimal TotalDebit, decimal TotalCredit, decimal EndingBalance);
public sealed record AgingDto(IReadOnlyList<AgingBucketDto> Buckets, decimal Total);

public interface IPartnerReadService
{
    Task<IReadOnlyList<PartnerRowDto>> GetListAsync(string? search, int page = 1, int pageSize = 100);
    Task<int> GetTotalCountAsync(string? search);
    Task<PartnerDetailDto?> GetDetailAsync(int id);
    Task<StatementDto> BuildStatementAsync(int partnerId, DateOnly? from, DateOnly? to);
    Task<AgingDto> BuildAgingAsync(int partnerId, DateOnly asOf);
}

public interface IPartnerCommandService
{
    Task<int> CreateAsync(PartnerDetailDto dto);
    Task UpdateAsync(PartnerDetailDto dto);
    Task DeleteAsync(int id);
}

public interface IPartnerExportService
{
    Task<byte[]> ExportStatementExcelAsync(int partnerId, DateOnly? from, DateOnly? to);
    Task<byte[]> ExportStatementPdfAsync(int partnerId, DateOnly? from, DateOnly? to, bool includeClosed);
    Task<byte[]> ExportAgingExcelAsync(int partnerId, DateOnly asOf);
    Task<byte[]> ExportAgingPdfAsync(int partnerId, DateOnly asOf);
}

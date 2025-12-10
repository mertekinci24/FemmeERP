using InventoryERP.Application.Cash;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Partners;
using InventoryERP.Application.Products;
// R-072: Removed Infrastructure.Abstractions using (IFileLoggerService deleted)
using InventoryERP.Infrastructure.Queries;
using InventoryERP.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using MediatR;

namespace InventoryERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // R-072: Removed IFileLoggerService registration (R-056 strategy abandoned in R-069)


        // R-072: Removed IFileLoggerService registration (R-056 strategy abandoned in R-069)
        
        // MediatR/AutoMapper/Validators kayıtların yanına ekle
        services.AddMediatR(typeof(DependencyInjection).Assembly);
        services.AddScoped<IDocumentQueries, DocumentQueries>();
        services.AddScoped<IPrintService, PrintService>();
        // Dashboard queries (R-024)
        services.AddScoped<InventoryERP.Application.Reports.IDashboardQueries, Infrastructure.Queries.DashboardQueriesEf>();
        services.AddScoped<IInvoiceCommandService, InvoiceCommandService>();
        services.AddScoped<IInvoicePostingService, InvoicePostingService>();
        services.AddScoped<InventoryERP.Application.Documents.IDocumentCommandService, DocumentCommandService>();
        services.AddScoped<InventoryERP.Application.Documents.INumberSequenceService, NumberSequenceService>(); // R-203
        // R-104: Changed from Scoped to Singleton - fixes DI lifetime mismatch with WPF ViewModels
        services.AddSingleton<IDocumentReportService, DocumentReportService>();
    services.AddScoped<IPartnerReadService, Infrastructure.Queries.PartnerReadService>();
    services.AddScoped<IPartnerCommandService, PartnerCommandService>();
    services.AddScoped<ICashService, CashService>();
    // R-086: Partner CRUD service
    services.AddScoped<InventoryERP.Application.Partners.IPartnerService, Infrastructure.Partners.PartnerService>();
    // R-020: Stocks / Partners report & export services
    services.AddScoped<InventoryERP.Application.Stocks.IStockQueries, Infrastructure.Queries.StockQueriesEf>();
    // Export/report services are stateless and safe to create transiently
    services.AddTransient<InventoryERP.Application.Stocks.IStockExportService, Infrastructure.Services.StockExportServiceClosedXml>();
    services.AddTransient<IPartnerExportService, PartnerExportService>();
    // Backup service
    services.AddTransient<InventoryERP.Application.Backup.IBackupService, Infrastructure.Services.BackupService>();
    services.AddTransient<InventoryERP.Application.Backup.IBackupValidator, Infrastructure.Services.BackupValidator>();
    // Landed cost service (R-031)
    services.AddTransient<Infrastructure.Services.ILandedCostService, Infrastructure.Services.LandedCostService>();
    // R-008: Simple reporting service
    services.AddTransient<InventoryERP.Application.Reports.IReportingService, Infrastructure.Services.ReportingService>();
    // R-032: Inventory Valuation
    services.AddTransient<InventoryERP.Application.Stocks.IInventoryValuationService, Infrastructure.Services.InventoryValuationService>();
    // R-033: CSV import service
    services.AddTransient<InventoryERP.Application.Import.IImportService, Infrastructure.Services.ImportService>();
    // R-093: Excel import service for Partners
    services.AddTransient<InventoryERP.Application.Import.IExcelImportService, Infrastructure.Services.ExcelImportService>();
    // R-095: List export services (Excel and PDF)
    services.AddTransient<InventoryERP.Application.Export.IExcelExportService, Infrastructure.Services.ExcelExportService>();
    services.AddTransient<InventoryERP.Application.Export.IListPdfExportService, Infrastructure.Services.ListPdfExportService>();
    // R-011: Mock e-invoice adapter
    services.AddTransient<InventoryERP.Application.EInvoicing.IEInvoiceAdapter, Infrastructure.Adapters.MockEInvoiceAdapter>();
    // R-041: Price list service
    services.AddScoped<IPriceListService, PriceListService>();
        services.AddSingleton<Infrastructure.Services.ICompanyService, Infrastructure.Services.CompanyService>();
        // Excel exporter/importer statik olduğu için DI gerekmez, ama istenirse singleton olarak eklenebilir
        // Validasyon
        services.AddScoped<FluentValidation.IValidator<InventoryERP.Application.Partners.PartnerDetailDto>, Infrastructure.Validators.PartnerValidator>();
        services.AddScoped<IProductsReadService, ProductsReadService>();
    // backup services registered above
        return services;
    }

    // R-107: Overload accepting configuration (currently unused but enables root App to pass IConfiguration for future options)
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // Future: bind additional Infrastructure-specific options from configuration if needed
        return services.AddInfrastructure();
    }
}

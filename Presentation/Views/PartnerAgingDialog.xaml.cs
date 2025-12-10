using System;
using System.Threading.Tasks;
using System.Windows;
using InventoryERP.Application.Partners;
using Microsoft.Win32;

namespace InventoryERP.Presentation.Views;

public partial class PartnerAgingDialog : Window
{
    private readonly int _partnerId;
    private readonly IPartnerReadService _readSvc;
    private readonly IPartnerExportService _exportSvc;

    public PartnerAgingDialog(int partnerId, IPartnerReadService readSvc, IPartnerExportService exportSvc)
    {
        InitializeComponent();
        _partnerId = partnerId;
        _readSvc = readSvc;
        _exportSvc = exportSvc;
        RefreshBtn.Click += async (_,__) => await LoadAsync();
        ExcelBtn.Click += async (_,__) => await ExportExcelAsync();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        DateOnly asOf = DateOnly.FromDateTime(DateTime.Today);
        if (AsOfDate.SelectedDate is DateTime dt) asOf = DateOnly.FromDateTime(dt);
        var dto = await _readSvc.BuildAgingAsync(_partnerId, asOf);
        Grid.ItemsSource = dto.Buckets;
    }

    private async Task ExportExcelAsync()
    {
        DateOnly asOf = DateOnly.FromDateTime(DateTime.Today);
        if (AsOfDate.SelectedDate is DateTime dt) asOf = DateOnly.FromDateTime(dt);
        var sfd = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = $"PartnerAging_{_partnerId}.xlsx" };
        if (sfd.ShowDialog() == true)
        {
            var bytes = await _exportSvc.ExportAgingExcelAsync(_partnerId, asOf);
            await System.IO.File.WriteAllBytesAsync(sfd.FileName, bytes);
            MessageBox.Show("Excel oluÅŸturuldu.");
        }
    }
}



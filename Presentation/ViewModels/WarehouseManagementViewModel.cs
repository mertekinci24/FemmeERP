// ReSharper disable once All
#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Presentation.Commands;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Presentation.ViewModels
{
	/// <summary>
	/// R-040: Warehouse Management ViewModel
	/// Manages warehouses and their locations (R-013, R-034)
	/// </summary>
	public sealed class WarehouseManagementViewModel : ViewModelBase
	{
		private readonly AppDbContext _db;

		public ObservableCollection<WarehouseRow> Warehouses { get; } = new();
		public ObservableCollection<LocationRow> Locations { get; } = new();

		private WarehouseRow? _selectedWarehouse;
		public WarehouseRow? SelectedWarehouse
		{
			get => _selectedWarehouse;
			set
			{
				if (SetProperty(ref _selectedWarehouse, value))
				{
					_ = LoadLocationsAsync(); // Fire and forget
				}
			}
		}

		public RelayCommand RefreshCmd { get; }
		public RelayCommand NewWarehouseCmd { get; }
		public RelayCommand NewLocationCmd { get; }
		public RelayCommand EditWarehouseCmd { get; }
		public RelayCommand DeleteWarehouseCmd { get; }

        public WarehouseManagementViewModel(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));

			RefreshCmd = new RelayCommand(async _ => await RefreshAsync());
			NewWarehouseCmd = new RelayCommand(async _ => await CreateWarehouseAsync());
			NewLocationCmd = new RelayCommand(async _ => await CreateLocationAsync());
			EditWarehouseCmd = new RelayCommand(async _ => await EditWarehouseAsync());
			DeleteWarehouseCmd = new RelayCommand(async _ => await DeleteWarehouseAsync());

            _ = LoadAsync(); // Fire and forget
        }

		private async Task LoadAsync()
		{
			await RefreshAsync();
		}

		private async Task RefreshAsync()
		{
			Warehouses.Clear();
			var warehouses = await _db.Warehouses
				.OrderBy(w => w.Code)
				.Select(w => new WarehouseRow(w.Id, w.Code, w.Name))
				.ToListAsync();
			
			foreach (var wh in warehouses)
			{
				Warehouses.Add(wh);
			}

			// Auto-select first warehouse
			if (Warehouses.Any())
			{
				SelectedWarehouse = Warehouses.First();
			}
		}

        private async Task LoadLocationsAsync()
        {
            Locations.Clear();

			if (SelectedWarehouse == null) return;

			var locations = await _db.Locations
				.Where(l => l.WarehouseId == SelectedWarehouse.Id)
				.OrderBy(l => l.Code)
				.Select(l => new LocationRow(l.Id, l.Code, l.Name))
				.ToListAsync();

            foreach (var loc in locations)
            {
                Locations.Add(loc);
            }
        }

		private async Task CreateWarehouseAsync()
        {
            var dlgCode = new Views.InputDialog("Yeni Depo", "Depo kodu (örn: WH1)");
            if (dlgCode.ShowDialog() != true) return;
            var code = dlgCode.ResultText?.Trim();
            if (string.IsNullOrWhiteSpace(code)) { System.Windows.MessageBox.Show("Kod boş olamaz."); return; }

            var dlgName = new Views.InputDialog("Yeni Depo", "Depo adı (örn: Merkez Depo)");
            if (dlgName.ShowDialog() != true) return;
            var name = dlgName.ResultText?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { System.Windows.MessageBox.Show("Ad boş olamaz."); return; }

            _db.Warehouses.Add(new InventoryERP.Domain.Entities.Warehouse { Code = code!, Name = name! });
            await _db.SaveChangesAsync();
            await RefreshAsync();
        }

		private async Task CreateLocationAsync()
        {
            if (SelectedWarehouse is null) { System.Windows.MessageBox.Show("Önce depo seçiniz."); return; }
            var dlgCode = new Views.InputDialog("Yeni Lokasyon", "Lokasyon kodu (örn: A1)");
            if (dlgCode.ShowDialog() != true) return;
            var code = dlgCode.ResultText?.Trim();
            if (string.IsNullOrWhiteSpace(code)) { System.Windows.MessageBox.Show("Kod boş olamaz."); return; }

            var dlgName = new Views.InputDialog("Yeni Lokasyon", "Lokasyon adı (örn: Raf 1)");
            if (dlgName.ShowDialog() != true) return;
            var name = dlgName.ResultText?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { System.Windows.MessageBox.Show("Ad boş olamaz."); return; }

            _db.Locations.Add(new InventoryERP.Domain.Entities.Location { WarehouseId = SelectedWarehouse.Id, Code = code!, Name = name! });
            await _db.SaveChangesAsync();
			await LoadLocationsAsync();
		}

		private async Task EditWarehouseAsync()
		{
			if (SelectedWarehouse is null) return;
			var dlgName = new Views.InputDialog("Depo Düzenle", "Depo adı", SelectedWarehouse.Name);
			if (dlgName.ShowDialog() != true) return;
			var name = dlgName.ResultText?.Trim();
			if (string.IsNullOrWhiteSpace(name)) { System.Windows.MessageBox.Show("Ad boş olamaz."); return; }

			var entity = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == SelectedWarehouse.Id);
			if (entity == null) return;
			entity.Name = name!;
			await _db.SaveChangesAsync();
			await RefreshAsync();
		}

		private async Task DeleteWarehouseAsync()
		{
			if (SelectedWarehouse is null) return;
			var res = System.Windows.MessageBox.Show($"{SelectedWarehouse.Name} silinsin mi?", "Onay", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
			if (res != System.Windows.MessageBoxResult.Yes) return;
			var wh = await _db.Warehouses.Include(w => w.Locations).FirstOrDefaultAsync(w => w.Id == SelectedWarehouse.Id);
			if (wh == null) return;
			_db.Locations.RemoveRange(wh.Locations);
			_db.Warehouses.Remove(wh);
			await _db.SaveChangesAsync();
			await RefreshAsync();
		}
	}

	// Display records
	public record WarehouseRow(int Id, string Code, string Name);
	public record LocationRow(int Id, string Code, string Name);
}


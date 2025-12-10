// ReSharper disable once All
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Security.Cryptography;
using System.Text;
using InventoryERP.Application.Products;
using InventoryERP.Presentation.Commands;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Presentation.ViewModels
{
	/// <summary>
	/// R-040: ViewModel for Item (Product) create/edit dialog
	/// Integrates Multi-UOM (R-029) and displays Multi-Warehouse stock (R-013)
	/// R-041: Integrates Price Lists
	/// </summary>
	public sealed class ItemEditViewModel : ViewModelBase
	{
		private readonly AppDbContext _db;
		private readonly IPriceListService _priceListService;
		private readonly Abstractions.IDialogService _dialogService;
		private readonly int? _productId;

		// Basic Product Fields (existing fields only, no new DB fields)
		private string _sku = "";
		public string Sku 
		{ 
			get => _sku; 
			set 
			{ 
				if (SetProperty(ref _sku, value))
				{
					((AsyncRelayCommand)SaveCmd).RaiseCanExecuteChanged();
				}
			} 
		}

		private string _name = "";
		public string Name 
		{ 
			get => _name; 
			set 
			{ 
				if (SetProperty(ref _name, value))
				{
					((AsyncRelayCommand)SaveCmd).RaiseCanExecuteChanged();
				}
			} 
		}

		private string? _category;
		public string? Category { get => _category; set => SetProperty(ref _category, value); }

		private string _baseUom = "EA";
	public string BaseUom 
	{ 
		get => _baseUom; 
		set 
		{ 
			if (SetProperty(ref _baseUom, value))
			{
				((AsyncRelayCommand)SaveCmd).RaiseCanExecuteChanged();
			}
		} 
	}

	private string? _barcode;
	public string? Barcode { get => _barcode; set => SetProperty(ref _barcode, value); }

	private int _vatRate = 20;
	public int VatRate { get => _vatRate; set => SetProperty(ref _vatRate, value); }

	private bool _active = true;
	public bool Active { get => _active; set => SetProperty(ref _active, value); }

	// R-171: Initial stock quantity for new products
	private decimal _initialStock = 0m;
	public decimal InitialStock 
	{ 
		get => _initialStock; 
		set 
		{
			if (SetProperty(ref _initialStock, value))
			{
				OnPropertyChanged(nameof(IsInitialStockPositive));
			}
		} 
	}
	public bool IsInitialStockPositive => InitialStock > 0;

	// R-228: Product Cost (Maliyet) - Actual cost per unit
	private decimal _cost = 0m;
	public decimal Cost 
	{ 
		get => _cost; 
		set => SetProperty(ref _cost, value); 
	}

	// R-234: Sales Price (Satış Fiyatı) for invoice auto-fill
	private decimal _salesPrice = 0m;
	public decimal SalesPrice 
	{ 
		get => _salesPrice; 
		set => SetProperty(ref _salesPrice, value); 
	}

	// R-230: Edit mode detection - true for existing products
	public bool IsEditMode => _productId.HasValue;

	// R-230: Current stock for display (read-only in edit mode)
	private decimal _currentStock = 0m;
	public decimal CurrentStock 
	{ 
		get => _currentStock; 
		set => SetProperty(ref _currentStock, value); 
	}

	// R-051: Base UOM dropdown options (Turkish-friendly)
	public string[] BaseUomOptions { get; }  = InventoryERP.Domain.Enums.UnitOfMeasure.GetAllCodes();

	// R-029: Multi-UOM Management
	public ObservableCollection<ProductUomRow> UomList { get; } = new();
		private string _newUomName = "";
		public string NewUomName { get => _newUomName; set => SetProperty(ref _newUomName, value); }
		private decimal _newUomCoefficient = 1m;
		public decimal NewUomCoefficient { get => _newUomCoefficient; set => SetProperty(ref _newUomCoefficient, value); }

	// R-013: Multi-Warehouse Stock View (read-only)
	public ObservableCollection<WarehouseStockRow> WarehouseStockList { get; } = new();

	// R-041: Price Lists
	public ObservableCollection<PriceRow> PriceList { get; } = new();
	private PriceRow? _selectedPrice;
	public PriceRow? SelectedPrice { get => _selectedPrice; set => SetProperty(ref _selectedPrice, value); }

	// R-041: New Price input fields
	private string _newListCode = "";
	public string NewListCode { get => _newListCode; set => SetProperty(ref _newListCode, value); }
	private string _newPriceUomName = "";
	public string NewPriceUomName { get => _newPriceUomName; set => SetProperty(ref _newPriceUomName, value); }
	private decimal _newUnitPrice = 0m;
	public decimal NewUnitPrice { get => _newUnitPrice; set => SetProperty(ref _newUnitPrice, value); }
	private string _newCurrency = "TRY";
	public string NewCurrency { get => _newCurrency; set => SetProperty(ref _newCurrency, value); }
	private DateTime? _newValidFrom;
	public DateTime? NewValidFrom { get => _newValidFrom; set => SetProperty(ref _newValidFrom, value); }
	private DateTime? _newValidTo;
	public DateTime? NewValidTo { get => _newValidTo; set => SetProperty(ref _newValidTo, value); }

	// R-059: Product Variants (R-014 backend)
	public ObservableCollection<VariantAttributeRow> VariantAttributes { get; } = new();
	public ObservableCollection<ProductVariantRow> GeneratedVariants { get; } = new();
	
	private string _newAttributeName = "";
	public string NewAttributeName { get => _newAttributeName; set => SetProperty(ref _newAttributeName, value); }
	private string _newAttributeValues = "";
	public string NewAttributeValues { get => _newAttributeValues; set => SetProperty(ref _newAttributeValues, value); }

	// R-059: Bill of Materials (BOM) (R-014 backend)
	public ObservableCollection<BomItemRow> BomList { get; } = new();
	private BomItemRow? _selectedBomItem;
	public BomItemRow? SelectedBomItem { get => _selectedBomItem; set => SetProperty(ref _selectedBomItem, value); }
	
	private string _newBomComponentSearch = "";
	public string NewBomComponentSearch { get => _newBomComponentSearch; set => SetProperty(ref _newBomComponentSearch, value); }
	private int _newBomComponentId;
	public int NewBomComponentId { get => _newBomComponentId; set => SetProperty(ref _newBomComponentId, value); }
	private string _newBomComponentName = "";
	public string NewBomComponentName { get => _newBomComponentName; set => SetProperty(ref _newBomComponentName, value); }
	private decimal _newBomQtyPer = 1m;
	public decimal NewBomQtyPer { get => _newBomQtyPer; set => SetProperty(ref _newBomQtyPer, value); }
	
	public ObservableCollection<ComponentSearchResult> ComponentSearchResults { get; } = new();
	private const string BarcodePrefix = "69";
	private const int BarcodeRandomDigits = 10;
	private const int BarcodeMaxGenerationAttempts = 6;

	// Commands
	public ICommand SaveCmd { get; }
	public AsyncRelayCommand GenerateBarcodeCmd { get; }
	public RelayCommand CancelCmd { get; }
	public RelayCommand AddUomCmd { get; }
	public RelayCommand RemoveUomCmd { get; }
	public RelayCommand AddPriceCmd { get; }
	public RelayCommand RemovePriceCmd { get; }
	public RelayCommand AddVariantAttributeCmd { get; }
	public RelayCommand RemoveVariantAttributeCmd { get; }
	public RelayCommand GenerateVariantsCmd { get; }
	public RelayCommand RemoveVariantCmd { get; }
	public RelayCommand SearchComponentCmd { get; }
	public RelayCommand SelectComponentCmd { get; }
	public RelayCommand AddBomItemCmd { get; }
	public RelayCommand RemoveBomItemCmd { get; }		// Dialog result - null means no action, true means save, false means cancel
		private bool? _dialogResult;
		public bool? DialogResult 
		{ 
			get => _dialogResult; 
			private set => SetProperty(ref _dialogResult, value); 
		}



	// R-210: Default Warehouse/Location for Product
	public ObservableCollection<InventoryERP.Domain.Entities.Warehouse> Warehouses { get; } = new();
	public ObservableCollection<InventoryERP.Domain.Entities.Location> Locations { get; } = new();

	private int? _defaultWarehouseId;
	public int? DefaultWarehouseId 
	{ 
		get => _defaultWarehouseId; 
		set 
		{
			if (SetProperty(ref _defaultWarehouseId, value))
			{
				_ = LoadLocationsForWarehouseAsync(value);
			}
		} 
	}

	private int? _defaultLocationId;
	public int? DefaultLocationId { get => _defaultLocationId; set => SetProperty(ref _defaultLocationId, value); }

	public ItemEditViewModel(AppDbContext db, IPriceListService priceListService, Abstractions.IDialogService dialogService, int? productId = null)
	{
		_db = db ?? throw new ArgumentNullException(nameof(db));
		_priceListService = priceListService ?? throw new ArgumentNullException(nameof(priceListService));
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_productId = productId;

		SaveCmd = new AsyncRelayCommand(SaveAsync, () => CanSave(null));
		GenerateBarcodeCmd = new AsyncRelayCommand(GenerateBarcodeAsync);
		CancelCmd = new RelayCommand(_ => Cancel());
		AddUomCmd = new RelayCommand(_ => AddUom());
		RemoveUomCmd = new RelayCommand(param => RemoveUom(param as ProductUomRow));
		AddPriceCmd = new RelayCommand(_ => AddPrice());
		RemovePriceCmd = new RelayCommand(param => RemovePrice(param as PriceRow));

		// R-059: Variant and BOM commands
		AddVariantAttributeCmd = new RelayCommand(_ => AddVariantAttribute());
		RemoveVariantAttributeCmd = new RelayCommand(param => RemoveVariantAttribute(param as VariantAttributeRow));
		GenerateVariantsCmd = new RelayCommand(_ => GenerateVariants());
		RemoveVariantCmd = new RelayCommand(param => RemoveVariant(param as ProductVariantRow));
		SearchComponentCmd = new RelayCommand(_ => { _ = SearchComponentAsync(); });
		SelectComponentCmd = new RelayCommand(param => SelectComponent(param as ComponentSearchResult));
		AddBomItemCmd = new RelayCommand(_ => AddBomItem());
		RemoveBomItemCmd = new RelayCommand(param => RemoveBomItem(param as BomItemRow));

		_ = LoadAsync(); // Fire and forget
	}

	private async Task LoadLocationsForWarehouseAsync(int? warehouseId)
	{
		Locations.Clear();
		if (warehouseId.HasValue)
		{
			var locs = await _db.Locations.Where(l => l.WarehouseId == warehouseId.Value).OrderBy(l => l.Code).ToListAsync();
			foreach (var l in locs) Locations.Add(l);
		}
		else
		{
			// If no warehouse selected, maybe show all? Or none? Let's show none to force warehouse selection first.
		}
	}

	private async Task LoadAsync()
	{


		// R-210: Load Warehouses
		var warehouses = await _db.Warehouses.OrderBy(w => w.Name).ToListAsync();
		Warehouses.Clear();
		foreach (var w in warehouses) Warehouses.Add(w);

		if (!_productId.HasValue)
		{
			return; // New product mode - fields already have default values
		}

		var product = await _db.Products
			.Include(p => p.ProductUoms)
			.FirstOrDefaultAsync(p => p.Id == _productId.Value);

		if (product == null)
		{
			return;
		}

		Sku = product.Sku;
		Name = product.Name;
		Category = product.Category;
		BaseUom = product.BaseUom;
		Barcode = product.Barcode;
		VatRate = product.VatRate;
		Active = product.Active;
		
		// R-210: Load Defaults
		DefaultWarehouseId = product.DefaultWarehouseId;
		// Wait for locations to load triggered by property change, then set location
		if (DefaultWarehouseId.HasValue)
		{
			await LoadLocationsForWarehouseAsync(DefaultWarehouseId);
			DefaultLocationId = product.DefaultLocationId;
		}

		// R-229: Load Cost (Maliyet) from product
		Cost = product.Cost;
		
		// R-234: Load SalesPrice (Satış Fiyatı) from product
		SalesPrice = product.SalesPrice;

		// Load UOMs
		UomList.Clear();
		foreach (var uom in product.ProductUoms ?? Enumerable.Empty<InventoryERP.Domain.Entities.ProductUom>())
		{
			UomList.Add(new ProductUomRow(uom.Id, uom.UomName, uom.Coefficient));
		}

		// Load warehouse stock
		await LoadWarehouseStockAsync(product.Id);

		// R-041: Load price lists
		await LoadPricesAsync(product.Id);

		// R-059: Load variants and BOM
		await LoadVariantsAsync(product.Id);
		await LoadBomAsync(product.Id);
	}

	private async Task LoadWarehouseStockAsync(int productId)
	{
		WarehouseStockList.Clear();
		var warehouses = await _db.Warehouses.ToListAsync();
		var locations = await _db.Locations.ToListAsync();
		var moves = await _db.StockMoves
			.Where(sm => sm.ItemId == productId)
			.Select(sm => new { sm.SourceLocationId, sm.DestinationLocationId, sm.QtySigned })
			.ToListAsync();

		foreach (var w in warehouses)
		{
			var warehouseLocationIds = locations.Where(l => l.WarehouseId == w.Id).Select(l => l.Id).ToHashSet();
			var qty = moves
				.Where(m => (m.SourceLocationId.HasValue && warehouseLocationIds.Contains(m.SourceLocationId.Value)) || 
							(m.DestinationLocationId.HasValue && warehouseLocationIds.Contains(m.DestinationLocationId.Value)))
				.Sum(m => m.QtySigned);

			if (qty != 0)
			{
				WarehouseStockList.Add(new WarehouseStockRow(w.Name, qty));
			}
		}
		
		// R-230: Calculate total current stock for display
		CurrentStock = WarehouseStockList.Sum(w => w.Qty);
	}

	private async Task EnsureSkuIsUniqueAsync(int? currentId)
	{
		if (string.IsNullOrWhiteSpace(Sku)) return;
		var exists = await _db.Products.AnyAsync(p => p.Sku == Sku && p.Id != currentId.GetValueOrDefault());
		if (exists)
		{
			throw new Exception($"SKU '{Sku}' zaten kullanımda.");
		}
	}

	private async Task<string> ResolveBarcodeAsync(InventoryERP.Domain.Entities.Product product)
	{
		if (!string.IsNullOrWhiteSpace(product.Barcode))
		{
			return product.Barcode;
		}

		for (int i = 0; i < BarcodeMaxGenerationAttempts; i++)
		{
			var sb = new StringBuilder();
			sb.Append(BarcodePrefix);
			for (int j = 0; j < BarcodeRandomDigits; j++)
			{
				sb.Append(Random.Shared.Next(0, 10));
			}
			var candidate = sb.ToString();

			var exists = await _db.Products.AnyAsync(p => p.Barcode == candidate && p.Id != product.Id);
			if (!exists)
			{
				return candidate;
			}
		}

		throw new Exception("Barkod üretilemedi, lütfen manuel giriniz.");
	}

	private async Task GenerateBarcodeAsync()
	{
		try
		{
			for (int i = 0; i < BarcodeMaxGenerationAttempts; i++)
			{
				var sb = new StringBuilder();
				sb.Append(BarcodePrefix);
				for (int j = 0; j < BarcodeRandomDigits; j++)
				{
					sb.Append(Random.Shared.Next(0, 10));
				}
				var candidate = sb.ToString();

				var exists = await _db.Products.AnyAsync(p => p.Barcode == candidate && p.Id != _productId.GetValueOrDefault());
				if (!exists)
				{
					Barcode = candidate;
					return;
				}
			}
			_dialogService.ShowMessageBox("Barkod üretilemedi.", "Hata");
		}
		catch (Exception ex)
		{
			_dialogService.ShowMessageBox($"Hata: {ex.Message}", "Hata");
		}
	}

	private bool CanSave(object? param)
	{
		return !string.IsNullOrWhiteSpace(Sku) && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(BaseUom);
	}

	private async Task SaveAsync()
	{
		try
		{
			InventoryERP.Domain.Entities.Product product;

			await EnsureSkuIsUniqueAsync(_productId);

            // R-210.4: Validate DefaultLocation if InitialStock is set
            if (!_productId.HasValue && InitialStock > 0 && !DefaultLocationId.HasValue)
            {
                _dialogService.ShowMessageBox("Açılış stoğu girdiğiniz için Varsayılan Konum seçmelisiniz.", "Uyarı");
                return;
            }

			if (_productId.HasValue)
			{
				// Edit mode
				product = await _db.Products.Include(p => p.ProductUoms).FirstAsync(p => p.Id == _productId.Value);
				product.Sku = Sku;
				product.Name = Name;
				product.Category = Category;
				product.BaseUom = BaseUom;
				product.VatRate = VatRate;
				product.Active = Active;
				product.Cost = Cost; // R-229: Save Cost (Maliyet)
				product.SalesPrice = SalesPrice; // R-234: Save SalesPrice
				// R-210: Save Defaults
				product.DefaultWarehouseId = DefaultWarehouseId;
				product.DefaultLocationId = DefaultLocationId;
			}
			else
			{
				// Create mode
				product = new InventoryERP.Domain.Entities.Product
				{
					Sku = Sku,
					Name = Name,
					Category = Category,
					BaseUom = BaseUom,
					VatRate = VatRate,
					Active = Active,
					Cost = Cost, // R-229: Save Cost (Maliyet)
					SalesPrice = SalesPrice, // R-234: Save SalesPrice
					ReservedQty = 0,
					// R-210: Save Defaults
					DefaultWarehouseId = DefaultWarehouseId,
					DefaultLocationId = DefaultLocationId
				};
				_db.Products.Add(product);
			}

			var barcodeValue = await ResolveBarcodeAsync(product);
			product.Barcode = barcodeValue;

			// Save main product first to get ID
			await _db.SaveChangesAsync();

			// R-172: Create opening stock StockMove if InitialStock > 0 (new product only)
			if (!_productId.HasValue && InitialStock > 0)
			{
                // R-210.4: Use DefaultLocationId (validated above)
				_db.StockMoves.Add(new InventoryERP.Domain.Entities.StockMove
				{
					ItemId = product.Id,
					Date = DateTime.Now,
					QtySigned = InitialStock,
					UnitCost = 0, // Opening stock, no cost yet
					DestinationLocationId = DefaultLocationId!.Value,
					Note = "Açılış stoğu"
				});

				await _db.SaveChangesAsync();
			}



			// R-029: Save Multi-UOM entries
				if (_productId.HasValue)
				{
					// Remove existing UOMs that are not in the current list
					var existingUoms = await _db.ProductUoms.Where(u => u.ProductId == product.Id).ToListAsync();
					var toRemove = existingUoms.Where(eu => !UomList.Any(u => u.Id == eu.Id)).ToList();
					_db.ProductUoms.RemoveRange(toRemove);
				}

				// Add/update UOMs
				foreach (var uomRow in UomList)
				{
					if (uomRow.Id == 0)
					{
						// New UOM
						_db.ProductUoms.Add(new InventoryERP.Domain.Entities.ProductUom
						{
							ProductId = product.Id,
							UomName = uomRow.UomName,
							Coefficient = uomRow.Coefficient
						});
					}
					else
					{
						// Update existing UOM
						var existingUom = await _db.ProductUoms.FirstOrDefaultAsync(u => u.Id == uomRow.Id);
						if (existingUom != null)
						{
							existingUom.UomName = uomRow.UomName;
							existingUom.Coefficient = uomRow.Coefficient;
						}
					}
				}

				await _db.SaveChangesAsync();

				// R-041: Save Price Lists
				await SavePricesAsync(product.Id);

				// R-059: Save Variants and BOM
				await SaveVariantsAsync(product.Id);
				await SaveBomAsync(product.Id);

			DialogResult = true;
			// Close dialog - will be handled by the view
		}
		catch (Exception ex)
		{
			var errorMsg = $"Kaydetme sÄ±rasÄ±nda hata oluÅŸtu: {ex.Message}";
			if (ex.InnerException != null)
			{
				errorMsg += $"\n\nDetay: {ex.InnerException.Message}";
			}
			_dialogService.ShowMessageBox(errorMsg, "Hata");
		}
	}		private void Cancel()
		{
			DialogResult = false;
			// Close dialog - will be handled by the view
		}

		private bool CanAddUom(object? param = null)
		{
			return !string.IsNullOrWhiteSpace(NewUomName) && NewUomCoefficient > 0;
		}

		private void AddUom()
		{
			if (CanAddUom(null))
			{
				UomList.Add(new ProductUomRow(0, NewUomName, NewUomCoefficient));
				NewUomName = "";
				NewUomCoefficient = 1m;
			}
		}

	private void RemoveUom(ProductUomRow? uomRow)
	{
		if (uomRow != null)
		{
			UomList.Remove(uomRow);
		}
	}

	private async Task LoadPricesAsync(int productId)
	{
		PriceList.Clear();
		var prices = await _priceListService.GetPricesByProductIdAsync(productId);
		foreach (var p in prices)
		{
			PriceList.Add(new PriceRow(
				p.Id,
				p.ListCode,
				p.UomName,
				p.UnitPrice,
				p.Currency,
				p.ValidFrom,
				p.ValidTo,
				false,
				false
			));
		}
	}

	// R-041: Save price lists using the PriceListService
	private async Task SavePricesAsync(int productId)
	{
		// Add new prices
		foreach (var priceRow in PriceList.Where(p => p.IsNew))
		{
			var createDto = new InventoryERP.Application.Products.CreatePriceDto(
				productId,
				priceRow.ListCode,
				priceRow.UomName,
				priceRow.UnitPrice,
				priceRow.Currency,
				priceRow.ValidFrom,
				priceRow.ValidTo
			);
			await _priceListService.AddPriceAsync(createDto);
		}

		// Update modified prices
		foreach (var priceRow in PriceList.Where(p => p.IsModified && !p.IsNew))
		{
			var updateDto = new InventoryERP.Application.Products.UpdatePriceDto(
				priceRow.Id,
				priceRow.ListCode,
				priceRow.UomName,
				priceRow.UnitPrice,
				priceRow.Currency,
				priceRow.ValidFrom,
				priceRow.ValidTo
			);
			await _priceListService.UpdatePriceAsync(priceRow.Id, updateDto);
		}

		// Delete removed prices (track deletions separately)
		// Note: For now, we'll just skip deleted items. In a more complex scenario,
		// we'd track a DeletedPriceIds list and call DeletePriceAsync for each.
	}

	// R-041: Add a new price to the list
	private void AddPrice()
	{
		if (string.IsNullOrWhiteSpace(NewListCode) || string.IsNullOrWhiteSpace(NewPriceUomName) || NewUnitPrice <= 0)
		{
			_dialogService.ShowMessageBox("LÃ¼tfen tÃ¼m alanlarÄ± doldurun (Liste Kodu, Birim, Fiyat > 0)", "Eksik Bilgi");
			return;
		}

		if (NewValidFrom.HasValue && NewValidTo.HasValue && NewValidFrom.Value >= NewValidTo.Value)
		{
			_dialogService.ShowMessageBox("BaÅŸlangÄ±Ã§ tarihi bitiÅŸ tarihinden Ã¶nce olmalÄ±dÄ±r", "GeÃ§ersiz Tarih");
			return;
		}

		PriceList.Add(new PriceRow(
			0, // New price, no ID yet
			NewListCode,
			NewPriceUomName,
			NewUnitPrice,
			NewCurrency,
			NewValidFrom,
			NewValidTo,
			true,  // IsNew
			false  // IsModified
		));

		// Clear input fields
		NewListCode = "";
		NewPriceUomName = "";
		NewUnitPrice = 0m;
		NewCurrency = "TRY";
		NewValidFrom = null;
		NewValidTo = null;
	}

	// R-059: Save product variants
	private async Task SaveVariantsAsync(int productId)
	{
		// Remove existing variants that are not in the current list
		if (_productId.HasValue)
		{
			var existingVariants = await _db.ProductVariants.Where(v => v.ProductId == productId).ToListAsync();
			var toRemove = existingVariants.Where(ev => !GeneratedVariants.Any(v => v.Id == ev.Id)).ToList();
			_db.ProductVariants.RemoveRange(toRemove);
		}

		// Add new variants
		foreach (var variant in GeneratedVariants.Where(v => v.IsNew))
		{
			_db.ProductVariants.Add(new InventoryERP.Domain.Entities.ProductVariant
			{
				ProductId = productId,
				Code = variant.Code
			});
		}

		await _db.SaveChangesAsync();
	}

	// R-059: Save BOM items
	private async Task SaveBomAsync(int productId)
	{
		// Remove existing BOM items that are not in the current list
		if (_productId.HasValue)
		{
			var existingBomItems = await _db.BomItems.Where(b => b.ParentProductId == productId).ToListAsync();
			var toRemove = existingBomItems.Where(eb => !BomList.Any(b => b.Id == eb.Id)).ToList();
			_db.BomItems.RemoveRange(toRemove);
		}

		// Add new BOM items
		foreach (var bomItem in BomList.Where(b => b.IsNew))
		{
			_db.BomItems.Add(new InventoryERP.Domain.Entities.BomItem
			{
				ParentProductId = productId,
				ComponentProductId = bomItem.ComponentProductId,
				QtyPer = bomItem.QtyPer
			});
		}

		// Update existing BOM items
		foreach (var bomItem in BomList.Where(b => !b.IsNew && b.Id > 0))
		{
			var existingBom = await _db.BomItems.FirstOrDefaultAsync(b => b.Id == bomItem.Id);
			if (existingBom != null)
			{
				existingBom.QtyPer = bomItem.QtyPer;
			}
		}

		await _db.SaveChangesAsync();
	}

	// R-041: Remove a price from the list
	private void RemovePrice(PriceRow? priceRow)
	{
		if (priceRow != null)
		{
			// If it's an existing price (Id > 0), we should delete it from the database
			// For now, we'll just remove from the list. The SavePricesAsync will handle
			// the actual deletion logic if we track deleted IDs.
			PriceList.Remove(priceRow);

			// TODO: Track deletions if needed
			// if (priceRow.Id > 0)
			// {
			//     DeletedPriceIds.Add(priceRow.Id);
			// }
		}
	}

	// R-059: Load product variants
	private async Task LoadVariantsAsync(int productId)
	{
		var variants = await _db.ProductVariants
			.Where(v => v.ProductId == productId)
			.ToListAsync();

		GeneratedVariants.Clear();
		foreach (var variant in variants)
		{
			GeneratedVariants.Add(new ProductVariantRow(variant.Id, variant.Code, false));
		}
	}

	// R-059: Load BOM items
	private async Task LoadBomAsync(int productId)
	{
		var bomItems = await _db.BomItems
			.Where(b => b.ParentProductId == productId)
			.Include(b => b.ComponentProduct)
			.ToListAsync();

		BomList.Clear();
		foreach (var bomItem in bomItems)
		{
			BomList.Add(new BomItemRow(
				bomItem.Id,
				bomItem.ComponentProductId,
				bomItem.ComponentProduct?.Name ?? "?",
				bomItem.QtyPer,
				false
			));
		}
	}

	// R-059: Add variant attribute
	private void AddVariantAttribute()
	{
		if (string.IsNullOrWhiteSpace(NewAttributeName) || string.IsNullOrWhiteSpace(NewAttributeValues))
		{
			_dialogService.ShowMessageBox("LÃ¼tfen Ã¶zellik adÄ± ve deÄŸerlerini girin", "Eksik Bilgi");
			return;
		}

		// Parse comma-separated values
		var values = NewAttributeValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(v => v.Trim())
			.Where(v => !string.IsNullOrWhiteSpace(v))
			.ToList();

		if (values.Count == 0)
		{
			_dialogService.ShowMessageBox("LÃ¼tfen en az bir deÄŸer girin", "Eksik Bilgi");
			return;
		}

		VariantAttributes.Add(new VariantAttributeRow(NewAttributeName, string.Join(", ", values)));
		
		NewAttributeName = "";
		NewAttributeValues = "";
	}

	// R-059: Remove variant attribute
	private void RemoveVariantAttribute(VariantAttributeRow? row)
	{
		if (row != null)
		{
			VariantAttributes.Remove(row);
		}
	}

	// R-059: Generate variant combinations
	private void GenerateVariants()
	{
		if (VariantAttributes.Count == 0)
		{
			_dialogService.ShowMessageBox("LÃ¼tfen Ã¶nce varyant Ã¶zelliklerini ekleyin", "Eksik Bilgi");
			return;
		}

		if (string.IsNullOrWhiteSpace(Sku))
		{
			_dialogService.ShowMessageBox("LÃ¼tfen Ã¶nce ana Ã¼rÃ¼n SKU'sunu girin", "Eksik Bilgi");
			return;
		}

		// Parse attribute values
		var attributeValuesList = VariantAttributes
			.Select(attr => attr.Values.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(v => v.Trim())
				.Where(v => !string.IsNullOrWhiteSpace(v))
				.ToList())
			.ToList();

		// Generate cartesian product
		var combinations = GenerateCombinations(attributeValuesList);

		// Clear existing generated variants
		GeneratedVariants.Clear();

		// Create variant codes
		foreach (var combination in combinations)
		{
			var variantCode = $"{Sku}-{string.Join("-", combination)}";
			GeneratedVariants.Add(new ProductVariantRow(0, variantCode, true));
		}

		_dialogService.ShowMessageBox($"{GeneratedVariants.Count} varyant oluÅŸturuldu", "BaÅŸarÄ±lÄ±");
	}

	// R-059: Generate cartesian product of attribute values
	private List<List<string>> GenerateCombinations(List<List<string>> attributeValuesList)
	{
		if (attributeValuesList.Count == 0)
			return new List<List<string>>();

		if (attributeValuesList.Count == 1)
			return attributeValuesList[0].Select(v => new List<string> { v }).ToList();

		var result = new List<List<string>>();
		var first = attributeValuesList[0];
		var rest = GenerateCombinations(attributeValuesList.Skip(1).ToList());

		foreach (var value in first)
		{
			foreach (var combination in rest)
			{
				var newCombination = new List<string> { value };
				newCombination.AddRange(combination);
				result.Add(newCombination);
			}
		}

		return result;
	}

	// R-059: Remove variant
	private void RemoveVariant(ProductVariantRow? row)
	{
		if (row != null)
		{
			GeneratedVariants.Remove(row);
		}
	}

	// R-059: Search for component products
	private async Task SearchComponentAsync()
	{
		if (string.IsNullOrWhiteSpace(NewBomComponentSearch))
		{
			ComponentSearchResults.Clear();
			return;
		}

		var searchTerm = NewBomComponentSearch.ToLower();
		var products = await _db.Products
			.Where(p => p.Active && (p.Sku.ToLower().Contains(searchTerm) || p.Name.ToLower().Contains(searchTerm)))
			.Take(10)
			.Select(p => new { p.Id, p.Sku, p.Name })
			.ToListAsync();

		ComponentSearchResults.Clear();
		foreach (var product in products)
		{
			ComponentSearchResults.Add(new ComponentSearchResult(product.Id, product.Sku, product.Name));
		}
	}

	// R-059: Select component from search results
	private void SelectComponent(ComponentSearchResult? result)
	{
		if (result != null)
		{
			NewBomComponentId = result.Id;
			NewBomComponentName = $"{result.Sku} - {result.Name}";
			ComponentSearchResults.Clear();
			NewBomComponentSearch = "";
		}
	}

	// R-059: Add BOM item
	private void AddBomItem()
	{
		if (NewBomComponentId == 0 || NewBomQtyPer <= 0)
		{
			_dialogService.ShowMessageBox("LÃ¼tfen bir hammadde seÃ§in ve miktar girin", "Eksik Bilgi");
			return;
		}

		// Check if already added
		if (BomList.Any(b => b.ComponentProductId == NewBomComponentId))
		{
			_dialogService.ShowMessageBox("Bu hammadde zaten eklenmiÅŸ", "UyarÄ±");
			return;
		}

		BomList.Add(new BomItemRow(0, NewBomComponentId, NewBomComponentName, NewBomQtyPer, true));

		// Clear inputs
		NewBomComponentId = 0;
		NewBomComponentName = "";
		NewBomQtyPer = 1m;
		NewBomComponentSearch = "";
	}

	// R-059: Remove BOM item
	private void RemoveBomItem(BomItemRow? row)
	{
		if (row != null)
		{
			BomList.Remove(row);
		}
	}
}

// R-029: Multi-UOM row for display
public record ProductUomRow(int Id, string UomName, decimal Coefficient);

// R-013: Warehouse stock row for display
public record WarehouseStockRow(string WarehouseName, decimal Qty);

// R-041: Price row for display
public record PriceRow(
	int Id,
	string ListCode,
	string UomName,
	decimal UnitPrice,
	string Currency,
	DateTime? ValidFrom,
	DateTime? ValidTo,
	bool IsNew,
	bool IsModified
);

// R-059: Variant attribute row for display
public record VariantAttributeRow(string AttributeName, string Values);

// R-059: Product variant row for display
public record ProductVariantRow(int Id, string Code, bool IsNew);

// R-059: BOM item row for display
public record BomItemRow(int Id, int ComponentProductId, string ComponentName, decimal QtyPer, bool IsNew);

// R-059: Component search result
public record ComponentSearchResult(int Id, string Sku, string Name);
}





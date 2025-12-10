using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Cmd = InventoryERP.Presentation.Commands;
using System.Threading.Tasks;
using System.Windows.Data;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Application.Partners;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums; // R-092: Required for PartnerType enum

namespace InventoryERP.Presentation.ViewModels;

public sealed class DocumentEditViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly InventoryERP.Application.Documents.IDocumentCommandService _cmd;
    private readonly IProductsReadService _productsSvc;
    private readonly IPartnerService? _partnerSvc; // R-092: Use new IPartnerService (R-086) instead of obsolete IPartnerReadService
    // R-072: Removed _logger field (R-056 strategy abandoned in R-069)
    private readonly Abstractions.IDialogService _dialogService; // R-069: Diagnostic dialog for exception capture
    // R-103 (R-101): PDF Export services - nullable for test compatibility, DI will provide in production
    private readonly InventoryERP.Application.Documents.IDocumentReportService? _reportService;
    private readonly Abstractions.IFileDialogService? _fileDialogService;
    public DocumentDetailDto Dto { get; }

    // R-044: Dynamic dialog title based on document type
    public string DocumentTitle
    {
        get
        {
            return Dto?.Type?.ToUpperInvariant() switch
            {
                "SALES_INVOICE" => "SatÄ±ÅŸ FaturasÄ±",
                "PURCHASE_INVOICE" => "AlÄ±ÅŸ FaturasÄ±",
                "ADJUSTMENT_OUT" => "Depo DÃ¼zeltme FiÅŸi (Ã‡Ä±kÄ±ÅŸ)",
                "ADJUSTMENT_IN" => "Depo DÃ¼zeltme FiÅŸi (GiriÅŸ)",
                "SAYIM_FISI" => "SayÄ±m FiÅŸi",
                "TRANSFER_FISI" => "Transfer FiÅŸi",
                "URETIM_FISI" => "Ãœretim FiÅŸi",
                "QUOTE" => "Teklif FiÅŸi",  // R-060: Quote document title
                "SALES_ORDER" => "SatÄ±ÅŸ SipariÅŸi",  // R-062/R-161: Sales Order document title
                _ => $"Belge DÃ¼zenle ({Dto?.Type ?? "Bilinmeyen"})"
            };
        }
    }

    public ObservableCollection<LineViewModel> Lines { get; } = new();
    private LineViewModel? _selectedLine;
    public LineViewModel? SelectedLine { get => _selectedLine; set { if (SetProperty(ref _selectedLine, value)) { try { RemoveLineCmd?.RaiseCanExecuteChanged(); } catch { } } } }
    public ObservableCollection<ProductRowDto> Products { get; } = new();
    public ObservableCollection<PartnerCrudListDto> Partners { get; } = new(); // R-092: Use PartnerCrudListDto (R-086) instead of obsolete PartnerRowDto
    
    // R-247: Warehouse selection for invoice header
    public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
    public int? WarehouseId
    {
        get => Dto.SourceWarehouseId;
        set
        {
            if (Dto.SourceWarehouseId != value)
            {
                Dto.SourceWarehouseId = value;
                OnPropertyChanged();
            }
        }
    }

    private ICollectionView? _productsView;
    public ICollectionView ProductsView => _productsView ??= CollectionViewSource.GetDefaultView(Products);

    private ICollectionView? _partnersView;
    public ICollectionView PartnersView => _partnersView ??= CollectionViewSource.GetDefaultView(Partners);

    private string? _searchText;
    private System.Threading.CancellationTokenSource? _cts;
    private readonly int _debounceMs;
    public event Action? ProductsRefreshed;
    public event Action? PartnersRefreshed;
    
    // R-044: Document type checks for conditional UI visibility
    public bool IsAdjustment => Dto?.Type?.StartsWith("ADJUSTMENT_", StringComparison.OrdinalIgnoreCase) == true || 
                                string.Equals(Dto?.Type, "SAYIM_FISI", StringComparison.OrdinalIgnoreCase);
    public bool IsTransfer => string.Equals(Dto?.Type, "TRANSFER_FISI", StringComparison.OrdinalIgnoreCase);
    public bool IsProduction => string.Equals(Dto?.Type, "URETIM_FISI", StringComparison.OrdinalIgnoreCase);
    // R-084 (refined): Partner required only for commercial docs (QUOTE, INVOICE types) not for transfers, production or lot-only flows
    private static readonly HashSet<string> PartnerRequiredTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "QUOTE",
        "SALES_ORDER",  // R-062/R-161: Sales Order requires partner selection
        "SALES_INVOICE",
        "PURCHASE_INVOICE"
        // Dispatch notes (SATIS_IRSALIYE, GELEN_IRSALIYE) may be internal; do not force partner in VM validation
    };
    public bool RequiresPartner => PartnerRequiredTypes.Contains(Dto?.Type ?? string.Empty); // Adjustments & operational docs excluded
    
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                // immediate refresh for tests/instant feedback, then debounce additional refresh to batch rapid typing
                try { ProductsView.Refresh(); } catch { }
                // Notify immediately so tests observing refreshes don't flake under timing
                try { ProductsRefreshed?.Invoke(); } catch { }
                try { _cts?.Cancel(); } catch { }
                _cts = new System.Threading.CancellationTokenSource();
                _ = DebounceRefreshAsync(_cts.Token);
            }
        }
    }

    private string? _partnerSearchText;
    public string? SearchPartnerText
    {
        get => _partnerSearchText;
        set
        {
            if (SetProperty(ref _partnerSearchText, value))
            {
                try { PartnersView.Refresh(); } catch { }
                try { PartnersRefreshed?.Invoke(); } catch { }
            }
        }
    }

    // Partner selection (Cari)
    public int? PartnerId
    {
        get => Dto.PartnerId;
        set
        {
            if (Dto.PartnerId != value)
            {
                Dto.PartnerId = value;
                OnPropertyChanged();
                UpdatePartnerTitleFromId();
                // R-160: Notify SaveCommand that CanExecute state may have changed
                try { SaveCommand?.RaiseCanExecuteChanged(); } catch { }
            }
        }
    }

    public string PartnerTitle
    {
        get => Dto.PartnerTitle;
        set
        {
            if (!string.Equals(Dto.PartnerTitle, value, StringComparison.Ordinal))
            {
                Dto.PartnerTitle = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    // R-246: Document header properties to fix data loss on edit (Bugs 1-3)
    public string DocumentNumber
    {
        get => Dto.Number;
        set
        {
            if (!string.Equals(Dto.Number, value, StringComparison.Ordinal))
            {
                Dto.Number = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    public DateTime Date
    {
        get => Dto.Date;
        set
        {
            if (Dto.Date != value)
            {
                Dto.Date = value;
                OnPropertyChanged();
            }
        }
    }

    public string Description
    {
        get => Dto.Description ?? string.Empty;
        set
        {
            if (!string.Equals(Dto.Description, value, StringComparison.Ordinal))
            {
                Dto.Description = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }


    private decimal _subtotal;
    public decimal Subtotal { get => _subtotal; private set => SetProperty(ref _subtotal, value); }
    private decimal _vatTotal;
    public decimal VatTotal { get => _vatTotal; private set => SetProperty(ref _vatTotal, value); }
    private decimal _grandTotal;
    public decimal GrandTotal { get => _grandTotal; private set => SetProperty(ref _grandTotal, value); }

    // INotifyDataErrorInfo
    private readonly Dictionary<string, List<string>> _errors = new();
    public bool HasErrors => _errors.Any();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    // Commands for dialog binding
    public Cmd.RelayCommand SaveCommand { get; }
    public Cmd.RelayCommand CancelCommand { get; }
    public Cmd.RelayCommand AddBarcodeCmd { get; }
    public Cmd.RelayCommand AddLineCmd { get; }
    public Cmd.RelayCommand RemoveLineCmd { get; }
    public Cmd.RelayCommand RemoveLineParamCmd { get; }
    public Cmd.RelayCommand MoveLineUpParamCmd { get; }
    public Cmd.RelayCommand MoveLineDownParamCmd { get; }
    public Cmd.RelayCommand MoveLineTopParamCmd { get; }
    public Cmd.RelayCommand MoveLineBottomParamCmd { get; }
    // R-008: PDF Export command for Quote documents
    public Cmd.RelayCommand ExportPdfCommand { get; }
    private string? _barcodeText;
    public string? BarcodeText { get => _barcodeText; set => SetProperty(ref _barcodeText, value); }

    // Events to notify host/dialog
    public event Action<bool>? SaveCompleted;
    public event Action? CancelRequested;

    public DocumentEditViewModel(
        DocumentDetailDto dto, 
       InventoryERP.Application.Documents.IDocumentCommandService cmd, 
        IProductsReadService productsSvc, 
        Abstractions.IDialogService dialogService, // R-069: Required for diagnostic exception display
       InventoryERP.Application.Documents.IDocumentReportService? reportService = null, // R-103 (R-101): Required for PDF export - nullable for test compatibility
        Abstractions.IFileDialogService? fileDialogService = null, // R-103 (R-101): Required for file save dialogs - nullable for test compatibility
        IPartnerService? partnerService = null, // R-092: Use new IPartnerService (R-086) instead of obsolete IPartnerReadService
        Microsoft.Extensions.Options.IOptions<InventoryERP.Presentation.Configuration.UiOptions>? uiOptions = null)
        // R-072: Removed IFileLoggerService parameter (R-056 strategy abandoned in R-069)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        Dto = dto;
        _cmd = cmd;
        _productsSvc = productsSvc;
        _partnerSvc = partnerService; // R-092: Store IPartnerService reference
        _dialogService = dialogService; // R-069: Store dialog service reference
        _reportService = reportService; // R-008: Store report service reference
        _fileDialogService = fileDialogService; // R-008: Store file dialog service reference
        // R-072: Removed all logger-related code (R-056 strategy abandoned)

        _debounceMs = uiOptions?.Value?.DebounceMs ??250;

        // R-103 UX: If editing an existing document with a partner, immediately reflect the name in the ComboBox text
        // This avoids a brief blank state before async partner load completes.
        if (!string.IsNullOrWhiteSpace(dto?.PartnerTitle))
        {
            SearchPartnerText = dto.PartnerTitle;
        }

        // initialize lines
    var dtoLines = Dto.Lines;
        if (dtoLines != null && dtoLines.Count > 0)
        {
            foreach (var l in dtoLines)
            {
                var vm = new LineViewModel(this, l);
                vm.PropertyChanged += LineViewModel_PropertyChanged;
                vm.ErrorsChanged += (_, __) =>
                {
                    // reflect child line errors at the view-level so INotifyDataErrorInfo.HasErrors is meaningful
                    if (Lines.Any(ll => ll.HasErrors)) SetError(nameof(Lines), "SatÄ±rlarda hatalar var."); else ClearErrors(nameof(Lines));
                    OnErrorsChanged(nameof(Lines));
                };
                Lines.Add(vm);
            }

            // If any child line already has errors (validation fired during construction), reflect that at the parent level.
            if (Lines.Any(l => l.HasErrors)) SetError(nameof(Lines), "SatÄ±rlarda hatalar var.");
        }
        else if (dtoLines != null && dtoLines.Count == 0)
        {
            // R-063: Pre-populate ONE empty line for new documents to activate the grid
            // This fixes the issue where DataGrid with CanUserAddRows=True doesn't show
            // the "add new row" functionality when ItemsSource is empty
            var emptyLine = new InventoryERP.Application.Documents.DTOs.DocumentLineDto
            {
                Qty = 0,
                UnitPrice = 0,
                VatRate = 20, // Default VAT rate (valid per CK_Product_VatRate constraint)
                Coefficient = 1m
            };
            Dto.Lines.Add(emptyLine);
            
            var vm = new LineViewModel(this, emptyLine);
            vm.PropertyChanged += LineViewModel_PropertyChanged;
            vm.ErrorsChanged += (_, __) =>
            {
                if (Lines.Any(ll => ll.HasErrors)) SetError(nameof(Lines), "SatÄ±rlarda hatalar var."); else ClearErrors(nameof(Lines));
                OnErrorsChanged(nameof(Lines));
            };
            Lines.Add(vm);
        }

        Lines.CollectionChanged += Lines_CollectionChanged;

        // products view filter
        _productsView = CollectionViewSource.GetDefaultView(Products);
        _productsView.Filter = o =>
        {
            if (o is not ProductRowDto p) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            return (p.Name?.IndexOf(SearchText!, StringComparison.OrdinalIgnoreCase) ?? -1) >=0 || (p.Sku?.IndexOf(SearchText!, StringComparison.OrdinalIgnoreCase) ?? -1) >=0;
        };

        // partners view filter
        _partnersView = CollectionViewSource.GetDefaultView(Partners);
        _partnersView.Filter = o =>
        {
            if (o is not PartnerCrudListDto pr) return false; // R-092: Use PartnerCrudListDto
            
            // R-094: ALWAYS show the selected partner (don't filter it out)
            // This prevents the selected partner from disappearing when SearchPartnerText doesn't match
            // (WPF IsEditable ComboBox updates Text after selection, triggering filter refresh)
            if (PartnerId.HasValue && pr.Id == PartnerId.Value) return true;
            
            if (string.IsNullOrWhiteSpace(SearchPartnerText)) return true;
            // R-092: Use new schema properties (Name, TaxId, NationalId)
            return (pr.Name?.IndexOf(SearchPartnerText!, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (pr.TaxId?.IndexOf(SearchPartnerText!, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (pr.NationalId?.IndexOf(SearchPartnerText!, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        };

        // When products collection changes (initial async load or subsequent refresh),
        // reconcile any lines that already have ItemId but missing details
        Products.CollectionChanged += (_, __) =>
        {
            _ = ReconcileLinesWithProductsAsync();
        };

    // fire-and-forget load products (UI can show while loading)
    // R-257: Use InitializeAsync pattern to properly await data loading
        _ = InitializeDataAsync();

        RecalcTotals();

        // commands
        // R-160: SaveCommand.CanExecute must prevent saving when partner is required but not selected
        SaveCommand = new Cmd.RelayCommand(async _ =>
        {
            var ok = await SaveAsync();
            SaveCompleted?.Invoke(ok);
        }, 
        _ => !RequiresPartner || (PartnerId.HasValue && PartnerId.Value > 0));
        CancelCommand = new Cmd.RelayCommand(_ =>
        {
            CancelRequested?.Invoke();
        });
        AddBarcodeCmd = new Cmd.RelayCommand(async _ => await AddBarcodeAsync());
        AddLineCmd = new Cmd.RelayCommand(_ => AddEmptyLine());
        RemoveLineCmd = new Cmd.RelayCommand(_ => RemoveSelectedLine(), _ => SelectedLine != null);
        RemoveLineParamCmd = new Cmd.RelayCommand(p => { if (p is LineViewModel l) RemoveSpecificLine(l); });
        MoveLineUpParamCmd = new Cmd.RelayCommand(p => { if (p is LineViewModel l) MoveLineRelative(l, -1); });
        MoveLineDownParamCmd = new Cmd.RelayCommand(p => { if (p is LineViewModel l) MoveLineRelative(l, +1); });
        MoveLineTopParamCmd = new Cmd.RelayCommand(p => { if (p is LineViewModel l) MoveLineAbsolute(l, 0); });
        MoveLineBottomParamCmd = new Cmd.RelayCommand(p => { if (p is LineViewModel l) MoveLineAbsolute(l, Lines.Count - 1); });
        // R-008: PDF Export command for Quote documents
        ExportPdfCommand = new Cmd.RelayCommand(async _ => await ExportPdfAsync(), 
            _ => Dto?.Id > 0 && Dto.Type == "QUOTE");
    }

    private async Task DebounceRefreshAsync(System.Threading.CancellationToken ct)
    {
        try
        {
            await Task.Delay(_debounceMs, ct);
            if (ct.IsCancellationRequested) return;
            // use DeferRefresh to batch any view changes; prefer dispatcher only if we already have access
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && dispatcher.CheckAccess())
            {
                using (ProductsView.DeferRefresh()) ProductsView.Refresh();
            }
            else
            {
                // fall back to direct refresh (tests and non-UI threads)
                using (ProductsView.DeferRefresh()) ProductsView.Refresh();
            }
        }
        catch (TaskCanceledException) { return; }
        catch { /* swallow for UI resilience */ }
        finally
        {
            if (!ct.IsCancellationRequested) ProductsRefreshed?.Invoke();
        }
    }

    private void Lines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (LineViewModel l in e.NewItems)
            {
                l.PropertyChanged += LineViewModel_PropertyChanged;
                l.ErrorsChanged += (_, __) =>
                {
                    if (Lines.Any(ll => ll.HasErrors)) SetError(nameof(Lines), "SatÄ±rlarda hatalar var."); else ClearErrors(nameof(Lines));
                    OnErrorsChanged(nameof(Lines));
                };
            }
        }
        if (e.OldItems != null)
        {
            foreach (LineViewModel l in e.OldItems)
            {
                l.PropertyChanged -= LineViewModel_PropertyChanged;
                l.ErrorsChanged -= (_, __) => { if (Lines.Any(ll => ll.HasErrors)) SetError(nameof(Lines), "SatÄ±rlarda hatalar var."); else ClearErrors(nameof(Lines)); OnErrorsChanged(nameof(Lines)); };
            }
        }
        RecalcTotals();
    }

    private void AddEmptyLine()
    {
        var dtoLine = new InventoryERP.Application.Documents.DTOs.DocumentLineDto
        {
            ItemId = 0,
            ItemName = string.Empty,
            Qty = 0m,
            UnitPrice = 0m,
            VatRate = 20,
            Uom = string.Empty,
            Coefficient = 1m
        };
        var vm = new LineViewModel(this, dtoLine);
        vm.PropertyChanged += LineViewModel_PropertyChanged;
        Lines.Add(vm);
        SelectedLine = vm;
        ClearErrors(nameof(Lines)); // allow user to fill new line
    }

    private void RemoveSelectedLine()
    {
        if (SelectedLine == null) return;
        var toRemove = SelectedLine;
        Lines.Remove(toRemove);
        SelectedLine = Lines.LastOrDefault();
        RecalcTotals();
        if (Lines.Count == 0)
        {
            // Always keep one placeholder line to simplify UI interactions
            AddEmptyLine();
        }
    }

    private void RemoveSpecificLine(LineViewModel line)
    {
        if (line == null) return;
        var idx = Lines.IndexOf(line);
        if (idx >= 0)
        {
            Lines.RemoveAt(idx);
            SelectedLine = Lines.Count > 0 ? Lines[Math.Min(idx, Lines.Count - 1)] : null;
            if (Lines.Count == 0) AddEmptyLine();
            RecalcTotals();
        }
    }

    private void MoveLineRelative(LineViewModel line, int delta)
    {
        var idx = Lines.IndexOf(line);
        if (idx < 0) return;
        var newIdx = Math.Max(0, Math.Min(Lines.Count - 1, idx + delta));
        MoveLineInternal(line, newIdx);
    }

    private void MoveLineAbsolute(LineViewModel line, int newIndex)
    {
        if (newIndex < 0) newIndex = 0;
        if (newIndex > Lines.Count - 1) newIndex = Lines.Count - 1;
        MoveLineInternal(line, newIndex);
    }

    private void MoveLineInternal(LineViewModel line, int newIndex)
    {
        var oldIndex = Lines.IndexOf(line);
        if (oldIndex < 0 || oldIndex == newIndex) return;
        Lines.RemoveAt(oldIndex);
        Lines.Insert(newIndex, line);
        SelectedLine = line;
    }

    private async Task LoadProductsAsync()
    {
        try
        {

            // R-282: Load Active Only to prevent clutter
            var allActive = await _productsSvc.GetListAsync(null, null, includePassive: false);
            
            // R-282: Identify "Ghost Products" (Passive items used in current lines)
            var usedIds = Dto?.Lines?.Select(l => l.ItemId).Distinct() ?? new List<int>();
            var loadedIds = allActive.Select(p => p.Id).ToHashSet();
            var missingIds = usedIds.Where(id => !loadedIds.Contains(id)).ToList();

            IEnumerable<InventoryERP.Application.Products.ProductRowDto> ghosts = new List<InventoryERP.Application.Products.ProductRowDto>();
            if (missingIds.Any())
            {
                // Fetch the missing passive products so the combo box can display them
                ghosts = await _productsSvc.GetByIdsAsync(missingIds);
            }

            // Combine Active + Ghosts
            Products.Clear();
            foreach (var p in allActive) Products.Add(p);
            foreach (var p in ghosts) Products.Add(p); // Add ghosts so they appear valid in this doc

            await ReconcileLinesWithProductsAsync();
        }
        catch { /* ignore for now */ }
    }

    private async Task LoadPartnersAsync()
    {
        try
        {
            if (_partnerSvc == null) return;
            Partners.Clear();
            
            // R-194.2: Filter partners by document type (Customers for sales, Suppliers for purchase)
            var docType = Dto?.Type ?? string.Empty;
            var targetType = PartnerType.Customer;
            if (docType.StartsWith("PURCHASE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(docType, "GELEN_IRSALIYE", StringComparison.OrdinalIgnoreCase))
            {
                targetType = PartnerType.Supplier;
            }

            var partners = await _partnerSvc.GetListAsync(filterByType: targetType);
            foreach (var p in partners) Partners.Add(p);
            
            // After loading, ensure PartnerTitle is in sync with PartnerId
            UpdatePartnerTitleFromId();
            
            // R-247: Notify PartnerId changed AFTER partners load so ComboBox rebinds
            OnPropertyChanged(nameof(PartnerId));
            
            // R-103 (R-102): Set SearchPartnerText to display partner name in ComboBox when editing existing document
            if (!string.IsNullOrWhiteSpace(PartnerTitle))
            {
                SearchPartnerText = PartnerTitle;
            }
        }
        catch (Exception ex)
        {
            // R-093: Add diagnostic logging instead of silent catch to identify loading failures
            System.Diagnostics.Debug.WriteLine($"[R-093 DIAGNOSTIC] LoadPartnersAsync failed: {ex}");
            try 
            { 
                _dialogService?.ShowMessageBox($"Cari yÃ¼kleme hatasÄ±:\n{ex.Message}", "R-093 Diagnostic"); 
            } 
            catch { /* dialog service might not be available in tests */ }
        }
        finally { try { PartnersRefreshed?.Invoke(); } catch { } }
    }

    // R-247: Load warehouses for invoice header selection
    private async Task LoadWarehousesAsync()
    {
        try
        {
            Warehouses.Clear();
            var warehouses = await _productsSvc.GetWarehousesAsync();
            foreach (var w in warehouses) Warehouses.Add(w);
            
            // Notify WarehouseId changed so ComboBox rebinds
            OnPropertyChanged(nameof(WarehouseId));
        }
        catch { /* ignore */ }
    }

    // R-251/R-257: Proper async initialization - wait for all data before refreshing bindings
    private async Task InitializeDataAsync()
    {
        try
        {
            // R-257: Wait for ALL async loads to complete before refreshing headers
            await Task.WhenAll(
                LoadProductsAsync(),
                LoadPartnersAsync(),
                LoadWarehousesAsync()
            );
            
            // NOW it is safe to refresh header bindings
            await RefreshHeaderBindingsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[R-257] InitializeDataAsync failed: {ex.Message}");
        }
    }

    // R-251/R-257/R-260/R-269: Force header refresh after async loads complete to fix empty fields on Edit
    private async Task RefreshHeaderBindingsAsync()
    {
        // R-269: Wait for collections to be populated (retry up to 5 times)
        int retry = 0;
        while ((this.Warehouses.Count == 0 || this.Partners.Count == 0) && retry < 5)
        {
            await Task.Delay(50);
            retry++;
        }
        
        // R-260: Force view refresh to ensure ComboBox sees loaded items
        try { PartnersView?.Refresh(); } catch { }
        try { if (_productsView != null) _productsView.Refresh(); } catch { }
        
        // R-260: Explicitly re-notify all header properties to force WPF rebinding
        // This is critical because ComboBox SelectedValue binding only works if collection is populated
        OnPropertyChanged(nameof(DocumentNumber));
        OnPropertyChanged(nameof(Date));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Partners));
        OnPropertyChanged(nameof(Warehouses));
        
        // R-269: Force Warehouse ComboBox selection with toggle trick
        if (this.WarehouseId.HasValue && this.WarehouseId.Value > 0)
        {
            var wh = this.Warehouses.FirstOrDefault(w => w.Id == this.WarehouseId.Value);
            if (wh != null)
            {
                // Toggle to force binding refresh if SelectedItem/SelectedValue desyncs
                var savedId = this.WarehouseId;
                Dto.SourceWarehouseId = null;
                OnPropertyChanged(nameof(WarehouseId));
                await Task.Delay(10); // Allow UI to process null
                Dto.SourceWarehouseId = savedId;
            }
        }
        OnPropertyChanged(nameof(WarehouseId));
        
        // R-269: Force Partner ComboBox selection with toggle trick  
        if (this.PartnerId.HasValue && this.PartnerId.Value > 0)
        {
            var partner = this.Partners.FirstOrDefault(p => p.Id == this.PartnerId.Value);
            if (partner != null)
            {
                var savedId = this.PartnerId;
                Dto.PartnerId = null;
                OnPropertyChanged(nameof(PartnerId));
                await Task.Delay(10);
                Dto.PartnerId = savedId;
                SearchPartnerText = partner.Name;
            }
        }
        OnPropertyChanged(nameof(PartnerId));
        OnPropertyChanged(nameof(PartnerTitle));
        
        System.Diagnostics.Debug.WriteLine($"[R-269] Header refresh complete: PartnerId={PartnerId}, PartnerCount={Partners.Count}, WarehouseId={WarehouseId}, WarehouseCount={Warehouses.Count}, Retries={retry}");
    }

    private async Task ReconcileLinesWithProductsAsync()
    {
        // R-060/R-064 QUOTE race condition fix (Yeni Teklif Ã¼rÃ¼n seÃ§imi gÃ¶rÃ¼nmÃ¼yor):
        // If user selected a product (ItemId set) BEFORE products finished loading,
        // LineViewModel_PropertyChanged could not resolve product details (ItemName, VatRate, UOM)
        // because Products collection was still empty. Whenever products are available/updated,
        // reconcile any lines that have ItemId>0 but missing ItemName/UOM.
        foreach (var l in Lines.Where(l => l.ItemId > 0 && string.IsNullOrWhiteSpace(l.ItemName)))
        {
            var prod = Products.FirstOrDefault(p => p.Id == l.ItemId);
            if (prod != null)
            {
                l.ItemName = prod.Name;
                l.VatRate = prod.VatRate;
                if (string.IsNullOrWhiteSpace(l.Uom))
                {
                    l.Uom = prod.BaseUom;
                    l.Coefficient = 1m;
                }
                try { await LoadUomsForLineAsync(l); } catch { }
                try { await LoadLotsForLineAsync(l); } catch { }
                try { await LoadVariantsForLineAsync(l); } catch { }
            }
        }
    }

    private void RecalcTotals()
    {
        Subtotal = Lines.Sum(l => l.LineNet);
        VatTotal = Lines.Sum(l => l.LineVat);
        GrandTotal = Lines.Sum(l => l.LineGross);
        OnErrorsChanged(null);
    }

    private void UpdatePartnerTitleFromId()
    {
        if (PartnerId.HasValue && PartnerId.Value > 0)
        {
            var pr = Partners.FirstOrDefault(x => x.Id == PartnerId.Value);
            if (pr != null)
            {
                PartnerTitle = pr.Name; // R-092: Use new schema property 'Name' instead of obsolete 'Title'
            }
            else
            {
                // keep existing title if not found; will reconcile after partners load
            }
        }
        else
        {
            PartnerTitle = string.Empty;
        }
    }

    private async void LineViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not LineViewModel l) return;
        
        // R-069: Removed IFileLoggerService diagnostics (never worked in UAT - R-066/R-067/R-068)
        // Using dialog box strategy (proven in R-052/R-057)
        try
        {
            if (e.PropertyName == nameof(LineViewModel.ItemId))
            {
                await LoadUomsForLineAsync(l);
                await LoadLotsForLineAsync(l);
                await LoadVariantsForLineAsync(l);
                
                // R-064: Auto-fill product details (ItemName, VatRate, BaseUom) when ItemId changes
                var prod = Products.FirstOrDefault(p => p.Id == l.ItemId);
                if (prod != null)
                {
                    // R-064 FIX: Update ItemName so the line doesn't "disappear" in the grid
                    l.ItemName = prod.Name;
                    
                    l.VatRate = prod.VatRate;
                    
                    // R-271: Only auto-fill SalesPrice for non-purchase documents
                    // Purchase documents require manual price entry (Cost not exposed in ProductRowDto)
                    bool isPurchase = Dto.Type.StartsWith("PURCHASE", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(Dto.Type, "GELEN_IRSALIYE", StringComparison.OrdinalIgnoreCase);
                    if (!isPurchase && l.UnitPrice == 0 && prod.SalesPrice > 0)
                    {
                        l.UnitPrice = prod.SalesPrice;
                    }
                    
                    // If UOM still not selected after loading available UOMs, fall back to product base UOM
                    if (string.IsNullOrWhiteSpace(l.Uom))
                    {
                        l.Uom = prod.BaseUom;
                        l.Coefficient = 1m;
                    }
                }
                else
                {
                    // Products may not be loaded yet (race). Attempt a lazy populate.
                    _ = TryPopulateLineFromProductsAsync(l);
                }
            }
        }
        catch (Exception ex)
        {
            // R-069: Show exception in dialog box (proven R-052 strategy)
            // Abandoning IFileLoggerService which never worked in UAT (R-066/R-067/R-068)
            _dialogService.ShowMessageBox(ex.ToString(), "R-069 DIAGNOSTIC");
        }
        finally
        {
            RecalcTotals();
        }
    }

    private async Task TryPopulateLineFromProductsAsync(LineViewModel l)
    {
        try
        {
            if (l.ItemId <= 0) return;
            // If Products not loaded yet, load once
            if (Products.Count == 0)
            {
                var list = await _productsSvc.GetListAsync(null);
                foreach (var p in list) Products.Add(p);
            }
            var prod = Products.FirstOrDefault(p => p.Id == l.ItemId);
            if (prod != null)
            {
                l.ItemName = prod.Name;
                l.VatRate = prod.VatRate;
                
                // R-271: Only auto-fill SalesPrice for non-purchase documents
                // Purchase documents require manual price entry (Cost not exposed in ProductRowDto)
                bool isPurchase = Dto.Type.StartsWith("PURCHASE", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(Dto.Type, "GELEN_IRSALIYE", StringComparison.OrdinalIgnoreCase);
                if (!isPurchase && l.UnitPrice == 0 && prod.SalesPrice > 0)
                {
                    l.UnitPrice = prod.SalesPrice;
                }
                
                if (string.IsNullOrWhiteSpace(l.Uom))
                {
                    l.Uom = prod.BaseUom;
                    l.Coefficient = 1m;
                }
                try { await LoadUomsForLineAsync(l); } catch { }
                try { await LoadLotsForLineAsync(l); } catch { }
                try { await LoadVariantsForLineAsync(l); } catch { }
            }
        }
        catch { /* swallow to avoid UI disruption */ }
        finally
        {
            RecalcTotals();
        }
    }

    private async Task LoadUomsForLineAsync(LineViewModel line)
    {
        // R-069: Removed try-catch to allow exception propagation to LineViewModel_PropertyChanged
        line.AvailableUoms.Clear();
        if (line.ItemId <= 0) return;
        var list = await _productsSvc.GetUomsAsync(line.ItemId);
        foreach (var u in list) line.AvailableUoms.Add(u);
        // if current Uom is empty and we have at least one uom, pick the first
        if (string.IsNullOrEmpty(line.Uom) && line.AvailableUoms.Count > 0)
        {
            line.Uom = line.AvailableUoms[0].UomName;
            line.Coefficient = line.AvailableUoms[0].Coefficient;
        }
        else
        {
            // ensure coefficient matches selected uom
            var match = line.AvailableUoms.FirstOrDefault(u => u.UomName == line.Uom);
            if (match != null) line.Coefficient = match.Coefficient;
        }
    }

    private async Task LoadLotsForLineAsync(LineViewModel line)
    {
        // R-069: Removed try-catch to allow exception propagation
        line.AvailableLots.Clear();
        if (line.ItemId <= 0) return;
        var list = await _productsSvc.GetLotsForProductAsync(line.ItemId);
        foreach (var lot in list) line.AvailableLots.Add(lot);
    }

    private async Task LoadVariantsForLineAsync(LineViewModel line)
    {
        // R-069: Removed try-catch to allow exception propagation
        line.AvailableVariants.Clear();
        line.ProductVariantId = null;
        if (line.ItemId <= 0) return;
        var list = await _productsSvc.GetVariantsAsync(line.ItemId);
        foreach (var v in list) line.AvailableVariants.Add(v);
        // don't auto-select to force explicit selection on production docs
    }

    // R-008: Export Quote as PDF
    private async Task ExportPdfAsync()
    {
        // R-099/R-103: Verify services are available (DI should provide them in production)
        if (_reportService == null || _fileDialogService == null)
        {
            _dialogService?.ShowMessageBox("PDF export servisleri yÃ¼klenemedi.", "Hata");
            return;
        }

        if (Dto == null || Dto.Id <= 0)
        {
            _dialogService?.ShowMessageBox("LÃ¼tfen Ã¶nce belgeyi kaydedin.", "UyarÄ±");
            return;
        }

        try
        {
            // Generate PDF
            var pdfBytes = await _reportService.GenerateQuotePdfAsync(Dto.Id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _dialogService?.ShowMessageBox("PDF oluÅŸturulamadÄ±.", "Hata");
                return;
            }

            // Show save file dialog
            var defaultFileName = $"Teklif_{Dto.Number}_{DateTime.Now:yyyyMMdd}.pdf";
            var filePath = _fileDialogService.ShowSaveFileDialog(
                defaultFileName,
                "PDF DosyalarÄ± (*.pdf)|*.pdf",
                "Teklifi PDF Olarak Kaydet");

            if (string.IsNullOrEmpty(filePath))
                return; // User cancelled

            // Save PDF to file
            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

            _dialogService?.ShowMessageBox($"PDF baÅŸarÄ±yla kaydedildi:\n{filePath}", "BaÅŸarÄ±lÄ±");
        }
        catch (Exception ex)
        {
            _dialogService?.ShowMessageBox($"PDF export hatasÄ±:\n{ex.Message}", "Hata");
        }
    }

    public IEnumerable<object> GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return _errors.SelectMany(kv => kv.Value);
        return _errors.TryGetValue(propertyName, out var list) ? list : Array.Empty<string>();
    }

    System.Collections.IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => GetErrors(propertyName);

    private void SetError(string prop, string error)
    {
        if (!_errors.TryGetValue(prop, out var list)) { list = new List<string>(); _errors[prop] = list; }
        if (!list.Contains(error)) { list.Add(error); OnErrorsChanged(prop); }
    }

    private void ClearErrors(string prop)
    {
        if (_errors.Remove(prop)) OnErrorsChanged(prop);
    }

    private void OnErrorsChanged(string? prop) => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));

    public async Task<bool> SaveAsync()
    {
        try
        {
            // view-level validation
            _errors.Clear();
            if (Lines.Count ==0) SetError(nameof(Lines), "En az bir satÄ±r gereklidir.");
            foreach (var l in Lines)
            {
                if (l.HasErrors) SetError(nameof(Lines), "SatÄ±rlarda hatalar var.");
            }
            // R-084: Require Partner selection for non-adjustment documents
            if (RequiresPartner && (!PartnerId.HasValue || PartnerId.Value <= 0))
            {
                SetError(nameof(PartnerId), "Cari seÃ§imi gereklidir.");
            }
            if (HasErrors) { OnErrorsChanged(null); return false; }

            // R-186: snapshot current DTO lines to allow rollback on failure
            var __prevDtoLines = Dto.Lines.ToList();
            Dto.Lines.Clear();
            foreach (var l in Lines)
            {
                Dto.Lines.Add(new DocumentLineDto
                {
                    Id = l.Id, // R-245: CRITICAL - without this, EF Core creates NEW rows instead of updating
                    ItemId = l.ItemId,
                    ItemName = l.ItemName,
                    Qty = l.Qty,
                    UnitPrice = l.UnitPrice,
                    Uom = l.Uom,
                    Coefficient = l.Coefficient,
                    VatRate = l.VatRate,
                    LineNet = l.LineNet,
                    LineVat = l.LineVat,
                    LineGross = l.LineGross,
                    ProductVariantId = l.ProductVariantId,
                    LotId = l.SelectedLotId,
                    LotNumber = string.IsNullOrWhiteSpace(l.NewLotNumber) ? null : l.NewLotNumber,
                    ExpiryDate = l.NewExpiryDate,
                    SourceLocationId = l.SourceLocationId,
                    DestinationLocationId = l.DestinationLocationId
                });
            }

            // R-057: Use single transaction method for ADJUSTMENT documents
            if (Dto.Type == "ADJUSTMENT_OUT" || Dto.Type == "ADJUSTMENT_IN")
            {
                await _cmd.SaveAndApproveAdjustmentAsync(Dto.Id, Dto);
            }
            else
            {
                await _cmd.UpdateDraftAsync(Dto.Id, Dto);
            }
            return true;
        }
        catch (Exception ex)
        {
            // R-160 R-069: Report exceptions via IDialogService (R-108) - do not swallow P0 errors
            try
            {
                await _dialogService.ShowErrorAsync("R-160 Kaydetme HatasÄ±", ex.ToString());
            }
            catch
            {
                // Fallback if dialog service fails
                System.Diagnostics.Debug.WriteLine($"[R-160 CRITICAL] SaveAsync failed: {ex}");
            }
                        // R-186: ensure DTO state is consistent after failure so user can retry
            try { Dto.Lines.Clear(); foreach (var l in Lines) { Dto.Lines.Add(new DocumentLineDto{ ItemId=l.ItemId, ItemName=l.ItemName, Qty=l.Qty, UnitPrice=l.UnitPrice, Uom=l.Uom, Coefficient=l.Coefficient, VatRate=l.VatRate, LineNet=l.LineNet, LineVat=l.LineVat, LineGross=l.LineGross, ProductVariantId=l.ProductVariantId, LotId=l.SelectedLotId, LotNumber= string.IsNullOrWhiteSpace(l.NewLotNumber)? null : l.NewLotNumber, ExpiryDate=l.NewExpiryDate, SourceLocationId=l.SourceLocationId, DestinationLocationId=l.DestinationLocationId }); } } catch { }
return false;
        }
    }

    private async Task AddBarcodeAsync()
    {
        var code = BarcodeText?.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;
        try
        {
            var prod = await _productsSvc.GetByCodeAsync(code);
            if (prod is null) return;

            var existing = Lines.FirstOrDefault(l => l.ItemId == prod.Id);
            if (existing is not null)
            {
                existing.Qty += 1m;
            }
            else
            {
                // R-070: Check if there's an empty line (R-063) that we should replace instead of adding new
                var emptyLine = Lines.FirstOrDefault(l => l.ItemId == 0 && l.Qty == 0);
                
                if (emptyLine is not null)
                {
                    // R-070: Replace the empty line placeholder with the scanned product
                    emptyLine.ItemId = prod.Id;
                    emptyLine.ItemName = prod.Name;
                    emptyLine.Qty = 1m;
                    emptyLine.Uom = prod.BaseUom;
                    emptyLine.Coefficient = 1m;
                    emptyLine.VatRate = prod.VatRate;
                    await LoadUomsForLineAsync(emptyLine);
                    await LoadLotsForLineAsync(emptyLine);
                }
                else
                {
                    // R-070: No empty line, add new line as usual
                    var dto = new InventoryERP.Application.Documents.DTOs.DocumentLineDto
                    {
                        ItemId = prod.Id,
                        ItemName = prod.Name,
                        Qty = 1m,
                        Uom = prod.BaseUom,
                        Coefficient = 1m,
                        UnitPrice = 0m,
                        VatRate = prod.VatRate
                    };
                    var vm = new LineViewModel(this, dto);
                    vm.PropertyChanged += LineViewModel_PropertyChanged;
                    Lines.Add(vm);
                    await LoadUomsForLineAsync(vm);
                    await LoadLotsForLineAsync(vm);
                }
            }
        }
        catch { /* ignore */ }
        finally
        {
            BarcodeText = string.Empty;
            RecalcTotals();
        }
    }

        public sealed class LineViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly DocumentEditViewModel _parent;
        
        // R-245: Line ID for update vs insert detection (Bug 6 fix)
        public int Id { get; set; }
        
        private int _itemId;
        public int ItemId { get => _itemId; set { if (_itemId != value) { _itemId = value; OnPropertyChanged(); Validate(); } } }
        
        // R-064: Make ItemName a proper notifying property so UI updates when product is selected
        private string _itemName = "";
        public string ItemName 
        { 
            get => _itemName; 
            set 
            { 
                if (_itemName != value) 
                { 
                    _itemName = value; 
                    OnPropertyChanged(); 
                } 
            } 
        }
        
        private decimal _qty;
        // R-241: Qty with full recalculation triggers
        public decimal Qty 
        { 
            get => _qty; 
            set 
            { 
                if (_qty != value) 
                { 
                    _qty = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(LineNet));
                    OnPropertyChanged(nameof(LineVat));
                    OnPropertyChanged(nameof(LineGross));
                    OnPropertyChanged(nameof(LineTotal)); 
                    Validate(); 
                } 
            } 
        }

        // UOM selection and conversion
        private string _uom = "";
        public string Uom
        {
            get => _uom;
            set
            {
                if (_uom != value)
                {
                    _uom = value;
                    // update Coefficient if we have a matching available uom
                    var match = AvailableUoms.FirstOrDefault(u => u.UomName == _uom);
                    Coefficient = match?.Coefficient ?? 1m;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineNet));
                    OnPropertyChanged(nameof(LineVat));
                    OnPropertyChanged(nameof(LineGross));
                    OnPropertyChanged(nameof(LineTotal));
                    Validate();
                }
            }
        }

        private decimal _unitPrice;
        // R-241: UnitPrice with full recalculation triggers
        public decimal UnitPrice 
        { 
            get => _unitPrice; 
            set 
            { 
                if (_unitPrice != value) 
                { 
                    _unitPrice = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(LineNet));
                    OnPropertyChanged(nameof(LineVat));
                    OnPropertyChanged(nameof(LineGross));
                    OnPropertyChanged(nameof(LineTotal)); 
                    Validate(); 
                } 
            } 
        }

        private decimal _coefficient = 1m;
        // R-241: Coefficient with full recalculation triggers
        public decimal Coefficient 
        { 
            get => _coefficient; 
            set 
            { 
                if (_coefficient != value) 
                { 
                    _coefficient = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(LineNet));
                    OnPropertyChanged(nameof(LineVat));
                    OnPropertyChanged(nameof(LineGross));
                    OnPropertyChanged(nameof(LineTotal)); 
                } 
            } 
        }

    public System.Collections.ObjectModel.ObservableCollection<InventoryERP.Application.Products.ProductUomDto> AvailableUoms { get; } = new System.Collections.ObjectModel.ObservableCollection<InventoryERP.Application.Products.ProductUomDto>();
        
        // R-241: VatRate with full recalculation triggers
        private int _vatRate;
        public int VatRate 
        { 
            get => _vatRate; 
            set 
            { 
                if (_vatRate != value) 
                { 
                    _vatRate = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(LineNet));
                    OnPropertyChanged(nameof(LineVat));
                    OnPropertyChanged(nameof(LineGross));
                    OnPropertyChanged(nameof(LineTotal)); 
                } 
            } 
        }

        // R-240: DiscountAmount for invoice line discounts
        private decimal _discountAmount;
        public decimal DiscountAmount 
        { 
            get => _discountAmount; 
            set 
            { 
                if (_discountAmount != value) 
                { 
                    _discountAmount = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(LineNet));
                    OnPropertyChanged(nameof(LineVat));
                    OnPropertyChanged(nameof(LineGross));
                    OnPropertyChanged(nameof(LineTotal)); 
                } 
            } 
        }

        // R-030: Lots per product
        public System.Collections.ObjectModel.ObservableCollection<InventoryERP.Application.Products.ProductLotDto> AvailableLots { get; } = new System.Collections.ObjectModel.ObservableCollection<InventoryERP.Application.Products.ProductLotDto>();
        private int? _selectedLotId;
        public int? SelectedLotId
        {
            get => _selectedLotId;
            set
            {
                if (_selectedLotId != value)
                {
                    _selectedLotId = value;
                    if (value.HasValue)
                    {
                        // Selecting an existing lot clears new lot entry
                        _newLotNumber = null;
                        _newExpiryDate = null;
                        OnPropertyChanged(nameof(NewLotNumber));
                        OnPropertyChanged(nameof(NewExpiryDate));
                    }
                    OnPropertyChanged();
                }
            }
        }
        private string? _newLotNumber;
        public string? NewLotNumber
        {
            get => _newLotNumber;
            set
            {
                if (_newLotNumber != value)
                {
                    _newLotNumber = value;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        // Typing a new lot clears existing selection
                        _selectedLotId = null;
                        OnPropertyChanged(nameof(SelectedLotId));
                    }
                    OnPropertyChanged();
                }
            }
        }
        private System.DateTime? _newExpiryDate;
        public System.DateTime? NewExpiryDate
        {
            get => _newExpiryDate;
            set
            {
                if (_newExpiryDate != value)
                {
                    _newExpiryDate = value;
                    if (value.HasValue)
                    {
                        _selectedLotId = null;
                        OnPropertyChanged(nameof(SelectedLotId));
                    }
                    OnPropertyChanged();
                }
            }
        }

        // R-240: LineNet includes discount
        public decimal LineNet => Math.Round((Qty * UnitPrice) - DiscountAmount, 2);
        public decimal LineVat => Math.Round(LineNet * VatRate /100m,2);
        public decimal LineGross => LineNet + LineVat;
        public decimal LineTotal => LineGross;

        // R-034: Location selection for transfers
        public sealed record LocationOption(int Id, string Code, string Name)
        {
            public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} - {Name}";
        }
        public System.Collections.ObjectModel.ObservableCollection<LocationOption> AvailableLocations { get; } = new System.Collections.ObjectModel.ObservableCollection<LocationOption>();
        private int? _sourceLocationId;
        public int? SourceLocationId { get => _sourceLocationId; set { if (_sourceLocationId != value) { _sourceLocationId = value; OnPropertyChanged(); Validate(); } } }
        private int? _destinationLocationId;
        public int? DestinationLocationId { get => _destinationLocationId; set { if (_destinationLocationId != value) { _destinationLocationId = value; OnPropertyChanged(); Validate(); } } }

    // R-035: Product variants per product, required for production documents
    public System.Collections.ObjectModel.ObservableCollection<InventoryERP.Application.Products.ProductVariantDto> AvailableVariants { get; } = new System.Collections.ObjectModel.ObservableCollection<InventoryERP.Application.Products.ProductVariantDto>();
    private int? _productVariantId;
    public int? ProductVariantId { get => _productVariantId; set { if (_productVariantId != value) { _productVariantId = value; OnPropertyChanged(); Validate(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // validation
        private readonly Dictionary<string, List<string>> _errs = new();
        public bool HasErrors => _errs.Any();
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public System.Collections.IEnumerable GetErrors(string? propertyName)
            => string.IsNullOrEmpty(propertyName) ? _errs.SelectMany(kv => kv.Value) : (_errs.TryGetValue(propertyName!, out var l) ? l : Array.Empty<string>());

        private void SetErrorLocal(string prop, string msg)
        {
            if (!_errs.TryGetValue(prop, out var list)) { list = new List<string>(); _errs[prop] = list; }
            if (!list.Contains(msg)) { list.Add(msg); ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop)); }
        }

        private void ClearErrorsLocal(string prop)
        {
            if (_errs.Remove(prop)) ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));
        }

        private void Validate()
        {
            // ItemId
            if (ItemId <=0) SetErrorLocal(nameof(ItemId), "ÃœrÃ¼n seÃ§iniz."); else ClearErrorsLocal(nameof(ItemId));
            // Qty
            if (Qty <=0) SetErrorLocal(nameof(Qty), "Miktar sÄ±fÄ±rdan bÃ¼yÃ¼k olmalÄ±dÄ±r."); else ClearErrorsLocal(nameof(Qty));
            // UnitPrice
            if (UnitPrice <0) SetErrorLocal(nameof(UnitPrice), "Birim fiyat negatif olamaz."); else ClearErrorsLocal(nameof(UnitPrice));

            // For transfer documents, require both source and destination locations and they must differ
            if (_parent.IsTransfer)
            {
                if (!SourceLocationId.HasValue) SetErrorLocal(nameof(SourceLocationId), "Kaynak lokasyon gerekli."); else ClearErrorsLocal(nameof(SourceLocationId));
                if (!DestinationLocationId.HasValue) SetErrorLocal(nameof(DestinationLocationId), "Hedef lokasyon gerekli."); else ClearErrorsLocal(nameof(DestinationLocationId));
                if (SourceLocationId.HasValue && DestinationLocationId.HasValue && SourceLocationId == DestinationLocationId)
                {
                    SetErrorLocal(nameof(DestinationLocationId), "Kaynak ve Hedef farklÄ± olmalÄ±dÄ±r.");
                }
            }

            // For production documents (URETIM_FISI), require a product variant selection on each line
            if (_parent.IsProduction)
            {
                if (!ProductVariantId.HasValue) SetErrorLocal(nameof(ProductVariantId), "Varyant seÃ§imi gereklidir."); else ClearErrorsLocal(nameof(ProductVariantId));
            }
        }

        public LineViewModel(DocumentEditViewModel parent, DocumentLineDto dto)
        {
            _parent = parent;
            Id = dto.Id; // R-245: Critical - preserve line ID for update detection
            _itemId = dto.ItemId;
            ItemName = dto.ItemName;
            _qty = dto.Qty;
            _unitPrice = dto.UnitPrice;
            _uom = dto.Uom;
            VatRate = dto.VatRate;
            _coefficient = dto.Coefficient;
            _productVariantId = dto.ProductVariantId;
            _sourceLocationId = dto.SourceLocationId;
            _destinationLocationId = dto.DestinationLocationId;
            Validate();
        }
    }
}












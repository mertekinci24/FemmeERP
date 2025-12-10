using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Xunit;
using InventoryERP.Infrastructure.Services;
using Moq;
using InventoryERP.Domain.Interfaces;

namespace Tests.Integration
{
    /// <summary>
    /// R-060: Integration tests for QUOTE document workflow
    /// R-061: Uses file-based SQLite (NOT InMemoryDatabase) for production parity
    /// Verifies that QUOTE documents do NOT create StockMove or PartnerLedgerEntry records
    /// </summary>
    public class QuoteWorkflowTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly SqliteConnection _connection;
        private readonly string _dbFilePath;

        public QuoteWorkflowTests()
        {
            // R-061: Use file-based SQLite with temp file (NOT UseInMemoryDatabase)
            _dbFilePath = Path.GetTempFileName() + ".db";
            _connection = new SqliteConnection($"Data Source={_dbFilePath}");
            _connection.Open();

            // Enable FK constraints (critical for production parity)
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = ON;";
                cmd.ExecuteNonQuery();
            }

            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            _db = new AppDbContext(opts);
            
            // R-061: Use Migrate() to apply all migrations (production parity)
            _db.Database.Migrate();
            
            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Create test partner
            var partner = new Partner
            {
                Title = "Test Customer",
                TaxNo = "1234567890",
                Role = PartnerRole.CUSTOMER
            };
            _db.Partners.Add(partner);

            // Create test product (R-061: Set valid VatRate to satisfy CK_Product_VatRate constraint)
            var product = new Product
            {
                Sku = "TEST-PRODUCT",
                Name = "Test Product",
                BaseUom = "PCS",
                Cost = 10m,
                VatRate = 20  // R-061: Must be 1, 10, or 20 (CHECK constraint)
            };
            _db.Products.Add(product);

            _db.SaveChanges();
        }

        public void Dispose()
        {
            _db?.Dispose();
            _connection?.Dispose();
            
            // R-061: Clean up temp database file
            if (File.Exists(_dbFilePath))
            {
                try { File.Delete(_dbFilePath); } catch { /* Ignore cleanup errors */ }
            }
        }

        [Fact]
        public async Task WhenCreatingQuoteDocument_ShouldSucceed()
        {
            // Arrange
            var partner = await _db.Partners.FirstAsync(p => p.Name == "Test Customer");
            var product = await _db.Products.FirstAsync(p => p.Sku == "TEST-PRODUCT");

            var quoteDoc = new Document
            {
                Type = DocumentType.QUOTE,  // R-060: QUOTE document type
                Number = "QT-2025-001",
                Date = DateTime.Today,
                Status = DocumentStatus.DRAFT,
                PartnerId = partner.Id,
                Currency = "TRY"
            };

            quoteDoc.Lines.Add(new DocumentLine
            {
                ItemId = product.Id,
                Qty = 5m,
                Coefficient = 1m,
                UnitPrice = 15m,
                Uom = "PCS",
                VatRate = 20
            });

            // Act
            _db.Documents.Add(quoteDoc);
            await _db.SaveChangesAsync();

            // Assert
            var savedDoc = await _db.Documents
                .Include(d => d.Lines)
                .FirstOrDefaultAsync(d => d.Number == "QT-2025-001");

            savedDoc.Should().NotBeNull();
            savedDoc!.Type.Should().Be(DocumentType.QUOTE);
            savedDoc.Lines.Should().HaveCount(1);
            savedDoc.Lines.First().Qty.Should().Be(5m);
            savedDoc.Lines.First().UnitPrice.Should().Be(15m);
        }

        [Fact]
        public async Task WhenApprovingQuoteDocument_ShouldNotCreateStockMoves()
        {
            // Arrange
            var partner = await _db.Partners.FirstAsync(p => p.Name == "Test Customer");
            var product = await _db.Products.FirstAsync(p => p.Sku == "TEST-PRODUCT");

            var quoteDoc = new Document
            {
                Type = DocumentType.QUOTE,
                Number = "QT-2025-002",
                Date = DateTime.Today,
                Status = DocumentStatus.DRAFT,
                PartnerId = partner.Id,
                Currency = "TRY"
            };

            quoteDoc.Lines.Add(new DocumentLine
            {
                ItemId = product.Id,
                Qty = 10m,
                Coefficient = 1m,
                UnitPrice = 15m,
                Uom = "PCS",
                VatRate = 20
            });

            _db.Documents.Add(quoteDoc);
            await _db.SaveChangesAsync();

            // Act: Approve the QUOTE document
            var posting = new InvoicePostingService(_db, new InventoryERP.Persistence.Services.InventoryQueriesEf(_db));
            await posting.ApproveAndPostAsync(quoteDoc.Id, null, null, default);

            // Assert 1: Document should be POSTED
            var postedDoc = await _db.Documents.FirstAsync(d => d.Id == quoteDoc.Id);
            postedDoc.Status.Should().Be(DocumentStatus.POSTED);

            // Assert 2: CRITICAL - NO StockMove records should be created for QUOTE
            var stockMoves = await _db.StockMoves
                .Where(sm => sm.DocLineId != null && _db.DocumentLines
                    .Where(dl => dl.DocumentId == quoteDoc.Id)
                    .Select(dl => dl.Id)
                    .Contains(sm.DocLineId.Value))
                .ToListAsync();

            stockMoves.Should().BeEmpty("QUOTE documents must NOT create StockMove records (R-060)");
        }

        [Fact]
        public async Task WhenApprovingQuoteDocument_ShouldNotCreatePartnerLedgerEntries()
        {
            // Arrange
            var partner = await _db.Partners.FirstAsync(p => p.Name == "Test Customer");
            var product = await _db.Products.FirstAsync(p => p.Sku == "TEST-PRODUCT");

            var quoteDoc = new Document
            {
                Type = DocumentType.QUOTE,
                Number = "QT-2025-003",
                Date = DateTime.Today,
                Status = DocumentStatus.DRAFT,
                PartnerId = partner.Id,
                Currency = "TRY"
            };

            quoteDoc.Lines.Add(new DocumentLine
            {
                ItemId = product.Id,
                Qty = 20m,
                Coefficient = 1m,
                UnitPrice = 15m,
                Uom = "PCS",
                VatRate = 20
            });

            _db.Documents.Add(quoteDoc);
            await _db.SaveChangesAsync();

            // Act: Approve the QUOTE document
            var posting = new InvoicePostingService(_db, new InventoryERP.Persistence.Services.InventoryQueriesEf(_db));
            await posting.ApproveAndPostAsync(quoteDoc.Id, null, null, default);

            // Assert 1: Document should be POSTED
            var postedDoc = await _db.Documents.FirstAsync(d => d.Id == quoteDoc.Id);
            postedDoc.Status.Should().Be(DocumentStatus.POSTED);

            // Assert 2: CRITICAL - NO PartnerLedgerEntry records should be created for QUOTE
            var ledgerEntries = await _db.PartnerLedgerEntries
                .Where(ple => ple.DocId == quoteDoc.Id)
                .ToListAsync();

            ledgerEntries.Should().BeEmpty("QUOTE documents must NOT create PartnerLedgerEntry records (R-060)");
        }

        [Fact]
        public async Task WhenApprovingQuoteDocument_ShouldOnlyChangeStatusToPosted()
        {
            // Arrange
            var partner = await _db.Partners.FirstAsync(p => p.Name == "Test Customer");
            var product = await _db.Products.FirstAsync(p => p.Sku == "TEST-PRODUCT");

            var quoteDoc = new Document
            {
                Type = DocumentType.QUOTE,
                Number = "QT-2025-004",
                Date = DateTime.Today,
                Status = DocumentStatus.DRAFT,
                PartnerId = partner.Id,
                Currency = "TRY"
            };

            quoteDoc.Lines.Add(new DocumentLine
            {
                ItemId = product.Id,
                Qty = 15m,
                Coefficient = 1m,
                UnitPrice = 15m,
                Uom = "PCS",
                VatRate = 20
            });

            _db.Documents.Add(quoteDoc);
            await _db.SaveChangesAsync();

            var stockMovesCountBefore = await _db.StockMoves.CountAsync();
            var ledgerEntriesCountBefore = await _db.PartnerLedgerEntries.CountAsync();

            // Act: Approve the QUOTE document
            var posting = new InvoicePostingService(_db, new InventoryERP.Persistence.Services.InventoryQueriesEf(_db));
            await posting.ApproveAndPostAsync(quoteDoc.Id, null, null, default);

            // Assert
            var postedDoc = await _db.Documents.FirstAsync(d => d.Id == quoteDoc.Id);
            postedDoc.Status.Should().Be(DocumentStatus.POSTED);

            var stockMovesCountAfter = await _db.StockMoves.CountAsync();
            var ledgerEntriesCountAfter = await _db.PartnerLedgerEntries.CountAsync();

            // Verify NO new StockMove or PartnerLedgerEntry records were created
            stockMovesCountAfter.Should().Be(stockMovesCountBefore, "QUOTE approval must not create StockMove records");
            ledgerEntriesCountAfter.Should().Be(ledgerEntriesCountBefore, "QUOTE approval must not create PartnerLedgerEntry records");
        }

        [Fact]
        public async Task WhenCreatingNewQuoteDocument_ShouldHaveOneEmptyLineForGridActivation()
        {
            // Arrange: Create a new QUOTE document with empty Lines collection (mimics QuotesViewModel.NewQuoteAsync)
            var partner = await _db.Partners.FirstAsync(p => p.Name == "Test Customer");
            
            var dto = new Application.Documents.DTOs.DocumentDetailDto
            {
                Type = "QUOTE",
                Number = "QT-2025-005",
                Date = DateTime.Today,
                Currency = "TRY",
                PartnerId = partner.Id,
                Lines = new System.Collections.Generic.List<Application.Documents.DTOs.DocumentLineDto>() // Empty list
            };

            // Act: Create DocumentEditViewModel (this should trigger R-063 fix to add empty line)
            var cmdSvc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(_db, new InventoryERP.Persistence.Services.InventoryQueriesEf(_db));
            var productsSvc = new global::InventoryERP.Infrastructure.Queries.ProductsReadService(_db);
            var vm = new InventoryERP.Presentation.ViewModels.DocumentEditViewModel(dto, cmdSvc, productsSvc, new Tests.Unit.TestHelpers.StubDialogService());

            // Assert 1: ViewModel should have 1 line (the pre-populated empty line for grid activation)
            vm.Lines.Should().HaveCount(1, "R-063: DocumentEditViewModel should add 1 empty line for grid activation");

            // Assert 2: The empty line should have default values
            var emptyLine = vm.Lines.First();
            emptyLine.Qty.Should().Be(0);
            emptyLine.UnitPrice.Should().Be(0);
            emptyLine.VatRate.Should().Be(20); // Default VAT rate
            emptyLine.Coefficient.Should().Be(1m);

            // Assert 3: dto.Lines should also have the empty line (synchronized)
            dto.Lines.Should().HaveCount(1, "R-063: dto.Lines should be updated with the empty line");
        }

        [Fact]
        public async Task WhenEditingExistingQuoteDocument_ShouldPreserveExistingLines()
        {
            // Arrange: Create QUOTE document with 2 lines already saved
            var partner = await _db.Partners.FirstAsync(p => p.Name == "Test Customer");
            var product = await _db.Products.FirstAsync(p => p.Sku == "TEST-PRODUCT");

            var quoteDoc = new Document
            {
                Type = DocumentType.QUOTE,
                Number = "QT-2025-006",
                Date = DateTime.Today,
                Status = DocumentStatus.DRAFT,
                PartnerId = partner.Id,
                Currency = "TRY"
            };

            quoteDoc.Lines.Add(new DocumentLine
            {
                ItemId = product.Id,
                Qty = 10m,
                Coefficient = 1m,
                UnitPrice = 100m,
                Uom = "PCS",
                VatRate = 20
            });

            quoteDoc.Lines.Add(new DocumentLine
            {
                ItemId = product.Id,
                Qty = 5m,
                Coefficient = 1m,
                UnitPrice = 200m,
                Uom = "PCS",
                VatRate = 20
            });

            _db.Documents.Add(quoteDoc);
            await _db.SaveChangesAsync();

            // Act: Load the document via IDocumentQueries.GetAsync (mimics editing existing doc)
            var queries = new global::InventoryERP.Infrastructure.Queries.DocumentQueries(_db);
            var dto = await queries.GetAsync(quoteDoc.Id);

            var cmdSvc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(_db, new InventoryERP.Persistence.Services.InventoryQueriesEf(_db));
            var productsSvc = new global::InventoryERP.Infrastructure.Queries.ProductsReadService(_db);
            var vm = new InventoryERP.Presentation.ViewModels.DocumentEditViewModel(dto!, cmdSvc, productsSvc, new Tests.Unit.TestHelpers.StubDialogService());

            // Assert: ViewModel should preserve the 2 existing lines (not add empty line)
            vm.Lines.Should().HaveCount(2, "R-063: Existing documents should preserve all lines");
            vm.Lines[0].Qty.Should().Be(10m);
            vm.Lines[0].UnitPrice.Should().Be(100m);
            vm.Lines[1].Qty.Should().Be(5m);
            vm.Lines[1].UnitPrice.Should().Be(200m);
        }
    }
}

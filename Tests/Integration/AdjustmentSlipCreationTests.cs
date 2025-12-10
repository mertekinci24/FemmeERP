using System;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// R-038: Integration test to diagnose and fix the "Depo Düzeltme Fiþi" (Adjustment Slip) save failure.
/// </summary>
public class AdjustmentSlipCreationTests : IDisposable
{
    private SqliteConnection? _connection;

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    [Fact]
    public async Task CreateDraftAsync_AdjustmentOut_WithEmptyLines_ShouldSucceed()
    {
        // Arrange
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;
        
        var svc = provider.GetRequiredService<IDocumentCommandService>();
        
        var dto = new DocumentDetailDto
        {
            Type = "ADJUSTMENT_OUT",
            Number = $"ADJ-{DateTime.Now:yyyyMMddHHmmss}",
            Date = DateTime.Today,
            PartnerId = null, // R-038 FIX: Explicitly test null PartnerId for adjustment slips
            Currency = "TRY",
            Lines = new System.Collections.Generic.List<DocumentLineDto>() // Empty lines
        };

        // Act & Assert
        // This should expose the inner exception if there's a database constraint violation
        Func<Task> act = async () => await svc.CreateDraftAsync(dto);
        
        // If this throws, we'll see the R-038 diagnostic message with inner exception
        await act.Should().NotThrowAsync("Creating an ADJUSTMENT_OUT document with empty lines should be valid");
    }

    [Fact]
    public async Task CreateDraftAsync_AdjustmentOut_WithLines_ShouldSucceed()
    {
        // Arrange
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;
        
        var svc = provider.GetRequiredService<IDocumentCommandService>();
        var db = provider.GetRequiredService<AppDbContext>();
        
        // Create a test product first
        var product = new Domain.Entities.Product
        {
            Sku = "TEST-001",
            Name = "Test Product",
            BaseUom = "EA",
            VatRate = 20,
            Active = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        
        var dto = new DocumentDetailDto
        {
            Type = "ADJUSTMENT_OUT",
            Number = $"ADJ-{DateTime.Now:yyyyMMddHHmmss}",
            Date = DateTime.Today,
            PartnerId = null, // R-038 FIX: Explicitly test null PartnerId for adjustment slips
            Currency = "TRY",
            Lines = new System.Collections.Generic.List<DocumentLineDto>
            {
                new DocumentLineDto
                {
                    ItemId = product.Id,
                    Qty = 10,
                    Uom = "EA",
                    UnitPrice = 0, // Adjustment typically has no price
                    VatRate = 20,
                    Coefficient = 1
                }
            }
        };

        // Act & Assert
        Func<Task> act = async () => await svc.CreateDraftAsync(dto);
        
        // If this throws, we'll see the R-038 diagnostic message with inner exception
        await act.Should().NotThrowAsync("Creating an ADJUSTMENT_OUT document with lines should be valid");
    }
}

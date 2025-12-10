using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Products;
using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Services;

/// <summary>
/// Implementation of IPriceListService using EF Core.
/// </summary>
public class PriceListService : IPriceListService
{
    private readonly AppDbContext _db;

    public PriceListService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PriceDto>> GetPricesByProductIdAsync(int productId)
    {
        var prices = await _db.Prices
            .Where(p => p.ProductId == productId)
            .OrderBy(p => p.ListCode)
            .ThenBy(p => p.UomName)
            .Select(p => new PriceDto(
                p.Id,
                p.ProductId,
                p.ListCode,
                p.UomName,
                p.UnitPrice,
                p.Currency,
                p.ValidFrom,
                p.ValidTo
            ))
            .ToListAsync();

        return prices;
    }

    public async Task<PriceDto> AddPriceAsync(CreatePriceDto dto)
    {
        // Validate product exists
        var productExists = await _db.Products.AnyAsync(p => p.Id == dto.ProductId);
        if (!productExists)
        {
            throw new ArgumentException($"Product with ID {dto.ProductId} does not exist.", nameof(dto.ProductId));
        }

        // Validate date range
        if (dto.ValidFrom.HasValue && dto.ValidTo.HasValue && dto.ValidFrom > dto.ValidTo)
        {
            throw new ArgumentException("ValidFrom must be before ValidTo.", nameof(dto.ValidFrom));
        }

        // Validate unit price
        if (dto.UnitPrice <= 0)
        {
            throw new ArgumentException("UnitPrice must be greater than zero.", nameof(dto.UnitPrice));
        }

        var price = new Price
        {
            ProductId = dto.ProductId,
            ListCode = dto.ListCode,
            UomName = dto.UomName,
            UnitPrice = dto.UnitPrice,
            Currency = dto.Currency,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo
        };

        _db.Prices.Add(price);
        await _db.SaveChangesAsync();

        return new PriceDto(
            price.Id,
            price.ProductId,
            price.ListCode,
            price.UomName,
            price.UnitPrice,
            price.Currency,
            price.ValidFrom,
            price.ValidTo
        );
    }

    public async Task<PriceDto> UpdatePriceAsync(int priceId, UpdatePriceDto dto)
    {
        var price = await _db.Prices.FindAsync(priceId);
        if (price == null)
        {
            throw new KeyNotFoundException($"Price with ID {priceId} not found.");
        }

        // Validate date range
        if (dto.ValidFrom.HasValue && dto.ValidTo.HasValue && dto.ValidFrom > dto.ValidTo)
        {
            throw new ArgumentException("ValidFrom must be before ValidTo.", nameof(dto.ValidFrom));
        }

        // Validate unit price
        if (dto.UnitPrice <= 0)
        {
            throw new ArgumentException("UnitPrice must be greater than zero.", nameof(dto.UnitPrice));
        }

        price.ListCode = dto.ListCode;
        price.UomName = dto.UomName;
        price.UnitPrice = dto.UnitPrice;
        price.Currency = dto.Currency;
        price.ValidFrom = dto.ValidFrom;
        price.ValidTo = dto.ValidTo;

        await _db.SaveChangesAsync();

        return new PriceDto(
            price.Id,
            price.ProductId,
            price.ListCode,
            price.UomName,
            price.UnitPrice,
            price.Currency,
            price.ValidFrom,
            price.ValidTo
        );
    }

    public async Task DeletePriceAsync(int priceId)
    {
        var price = await _db.Prices.FindAsync(priceId);
        if (price == null)
        {
            throw new KeyNotFoundException($"Price with ID {priceId} not found.");
        }

        _db.Prices.Remove(price);
        await _db.SaveChangesAsync();
    }

    public async Task<PriceDto?> GetEffectivePriceAsync(int productId, string listCode, string uomName, DateTime? date = null)
    {
        var effectiveDate = date ?? DateTime.Today;

        var price = await _db.Prices
            .Where(p => p.ProductId == productId)
            .Where(p => p.ListCode == listCode)
            .Where(p => p.UomName == uomName)
            .Where(p => 
                // ValidFrom is null OR ValidFrom <= effectiveDate
                (!p.ValidFrom.HasValue || p.ValidFrom.Value <= effectiveDate) &&
                // ValidTo is null OR ValidTo >= effectiveDate
                (!p.ValidTo.HasValue || p.ValidTo.Value >= effectiveDate)
            )
            .Select(p => new PriceDto(
                p.Id,
                p.ProductId,
                p.ListCode,
                p.UomName,
                p.UnitPrice,
                p.Currency,
                p.ValidFrom,
                p.ValidTo
            ))
            .FirstOrDefaultAsync();

        return price;
    }
}

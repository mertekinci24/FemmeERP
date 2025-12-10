using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Tests.Infrastructure;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Presentation.Abstractions;
using InventoryERP.Application.Products;
using InventoryERP.Domain.Entities;

namespace Tests.Unit;

/// <summary>
/// R-041: Unit tests for Price List functionality in ItemEditViewModel
/// Tests AddPriceCmd, RemovePriceCmd, LoadPricesAsync, SavePricesAsync
/// </summary>
public class ItemEditViewModelPriceTests : BaseIntegrationTest
{
	[Fact]
	public async Task AddPriceCmd_AddsNewPriceToCollection_AndClearsInputs()
	{
		// Arrange
		var product = new Product { Sku = "TEST-P1", Name = "Test Product", BaseUom = "EA", VatRate = 10, ReservedQty = 0 };
		Ctx.Products.Add(product);
		await Ctx.SaveChangesAsync();

		var mockPriceService = new Mock<IPriceListService>();
		mockPriceService.Setup(s => s.GetPricesByProductIdAsync(It.IsAny<int>()))
			.ReturnsAsync(new List<PriceDto>());

		var mockDialogService = new Mock<IDialogService>();

		var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, product.Id);
		await Task.Delay(100); // Wait for LoadAsync to complete

		// Set input fields
		vm.NewListCode = "NAKÝT";
		vm.NewPriceUomName = "EA";
		vm.NewUnitPrice = 100.00m;
		vm.NewCurrency = "TRY";
		vm.NewValidFrom = new DateTime(2025, 1, 1);
		vm.NewValidTo = new DateTime(2025, 12, 31);

		var initialCount = vm.PriceList.Count;

		// Act
		vm.AddPriceCmd.Execute(null);

		// Assert
		vm.PriceList.Count.Should().Be(initialCount + 1, "a new price should be added to the collection");

		var addedPrice = vm.PriceList.Last();
		addedPrice.ListCode.Should().Be("NAKÝT");
		addedPrice.UomName.Should().Be("EA");
		addedPrice.UnitPrice.Should().Be(100.00m);
		addedPrice.Currency.Should().Be("TRY");
		addedPrice.ValidFrom.Should().Be(new DateTime(2025, 1, 1));
		addedPrice.ValidTo.Should().Be(new DateTime(2025, 12, 31));
		addedPrice.IsNew.Should().BeTrue("newly added prices should be marked as IsNew");
		addedPrice.IsModified.Should().BeFalse();

		// Verify input fields are cleared
		vm.NewListCode.Should().Be("", "NewListCode should be cleared after adding");
		vm.NewPriceUomName.Should().Be("", "NewPriceUomName should be cleared after adding");
		vm.NewUnitPrice.Should().Be(0m, "NewUnitPrice should be cleared after adding");
		vm.NewCurrency.Should().Be("TRY", "NewCurrency should reset to default");
		vm.NewValidFrom.Should().BeNull("NewValidFrom should be cleared after adding");
		vm.NewValidTo.Should().BeNull("NewValidTo should be cleared after adding");
	}

	[Fact]
	public async Task AddPriceCmd_ShowsValidationError_WhenListCodeIsEmpty()
	{
		// Arrange
		var product = new Product { Sku = "TEST-P2", Name = "Test Product 2", BaseUom = "EA", VatRate = 10, ReservedQty = 0 };
		Ctx.Products.Add(product);
		await Ctx.SaveChangesAsync();

		var mockPriceService = new Mock<IPriceListService>();
		mockPriceService.Setup(s => s.GetPricesByProductIdAsync(It.IsAny<int>()))
			.ReturnsAsync(new List<PriceDto>());

		var mockDialogService = new Mock<IDialogService>();

		var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, product.Id);
		await Task.Delay(100); // Wait for LoadAsync

		// Set incomplete input (missing ListCode)
		vm.NewListCode = ""; // Empty
		vm.NewPriceUomName = "EA";
		vm.NewUnitPrice = 100.00m;

		var initialCount = vm.PriceList.Count;

		// Act
		vm.AddPriceCmd.Execute(null);

		// Assert
		vm.PriceList.Count.Should().Be(initialCount, "no price should be added when validation fails");
	}

	[Fact]
	public async Task AddPriceCmd_ShowsValidationError_WhenUnitPriceIsZero()
	{
		// Arrange
		var product = new Product { Sku = "TEST-P3", Name = "Test Product 3", BaseUom = "EA", VatRate = 10, ReservedQty = 0 };
		Ctx.Products.Add(product);
		await Ctx.SaveChangesAsync();

		var mockPriceService = new Mock<IPriceListService>();
		mockPriceService.Setup(s => s.GetPricesByProductIdAsync(It.IsAny<int>()))
			.ReturnsAsync(new List<PriceDto>());

		var mockDialogService = new Mock<IDialogService>();

		var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, product.Id);
		await Task.Delay(100);

		// Set invalid input (UnitPrice = 0)
		vm.NewListCode = "VADELÝ";
		vm.NewPriceUomName = "EA";
		vm.NewUnitPrice = 0m; // Invalid
		vm.NewCurrency = "TRY";

		var initialCount = vm.PriceList.Count;

		// Act
		vm.AddPriceCmd.Execute(null);

		// Assert
		vm.PriceList.Count.Should().Be(initialCount, "no price should be added when unit price is zero");
	}

	[Fact]
	public async Task AddPriceCmd_ShowsValidationError_WhenValidFromIsAfterValidTo()
	{
		// Arrange
		var product = new Product { Sku = "TEST-P4", Name = "Test Product 4", BaseUom = "EA", VatRate = 10, ReservedQty = 0 };
		Ctx.Products.Add(product);
		await Ctx.SaveChangesAsync();

		var mockPriceService = new Mock<IPriceListService>();
		mockPriceService.Setup(s => s.GetPricesByProductIdAsync(It.IsAny<int>()))
			.ReturnsAsync(new List<PriceDto>());

		var mockDialogService = new Mock<IDialogService>();

		var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, product.Id);
		await Task.Delay(100);

		// Set invalid date range
		vm.NewListCode = "BAYÝ";
		vm.NewPriceUomName = "EA";
		vm.NewUnitPrice = 95m;
		vm.NewCurrency = "USD";
		vm.NewValidFrom = new DateTime(2025, 12, 31); // After ValidTo
		vm.NewValidTo = new DateTime(2025, 1, 1);     // Before ValidFrom

		var initialCount = vm.PriceList.Count;

		// Act
		vm.AddPriceCmd.Execute(null);

		// Assert
		vm.PriceList.Count.Should().Be(initialCount, "no price should be added when ValidFrom >= ValidTo");
	}

	[Fact]
	public async Task RemovePriceCmd_RemovesPriceFromCollection()
	{
		// Arrange
		var product = new Product { Sku = "TEST-P5", Name = "Test Product 5", BaseUom = "EA", VatRate = 10, ReservedQty = 0 };
		Ctx.Products.Add(product);
		await Ctx.SaveChangesAsync();

		var existingPrices = new List<PriceDto>
		{
			new PriceDto(1, product.Id, "NAKÝT", "EA", 100m, "TRY", null, null),
			new PriceDto(2, product.Id, "VADELÝ", "EA", 110m, "TRY", null, null)
		};

		var mockPriceService = new Mock<IPriceListService>();
		mockPriceService.Setup(s => s.GetPricesByProductIdAsync(product.Id))
			.ReturnsAsync(existingPrices);

		var mockDialogService = new Mock<IDialogService>();

		var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, product.Id);
		await Task.Delay(100); // Wait for LoadAsync

		vm.PriceList.Count.Should().Be(2, "two prices should be loaded");

		var priceToRemove = vm.PriceList.First(p => p.ListCode == "NAKÝT");

		// Act
		vm.RemovePriceCmd.Execute(priceToRemove);

		// Assert
		vm.PriceList.Count.Should().Be(1, "one price should remain after removal");
		vm.PriceList.Should().NotContain(p => p.ListCode == "NAKÝT", "removed price should not be in collection");
		vm.PriceList.Should().Contain(p => p.ListCode == "VADELÝ", "other price should remain");
	}

	[Fact]
	public async Task LoadPricesAsync_PopulatesPriceListFromService()
	{
		// Arrange
		var product = new Product { Sku = "TEST-P6", Name = "Test Product 6", BaseUom = "EA", VatRate = 10, ReservedQty = 0 };
		Ctx.Products.Add(product);
		await Ctx.SaveChangesAsync();

		var mockPrices = new List<PriceDto>
		{
			new PriceDto(1, product.Id, "NAKÝT", "EA", 100m, "TRY", new DateTime(2025, 1, 1), new DateTime(2025, 6, 30)),
			new PriceDto(2, product.Id, "VADELÝ", "EA", 110m, "TRY", new DateTime(2025, 7, 1), null),
			new PriceDto(3, product.Id, "BAYÝ", "BOX", 95m, "USD", null, null)
		};

		var mockPriceService = new Mock<IPriceListService>();
		mockPriceService.Setup(s => s.GetPricesByProductIdAsync(product.Id))
			.ReturnsAsync(mockPrices);

		var mockDialogService = new Mock<IDialogService>();

		// Act
		var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, product.Id);
		await Task.Delay(100); // Wait for LoadAsync to complete

		// Assert
		mockPriceService.Verify(s => s.GetPricesByProductIdAsync(product.Id), Times.Once, 
			"GetPricesByProductIdAsync should be called exactly once during LoadAsync");

		vm.PriceList.Count.Should().Be(3, "all three prices should be loaded");

		var nakitPrice = vm.PriceList.First(p => p.ListCode == "NAKÝT");
		nakitPrice.UomName.Should().Be("EA");
		nakitPrice.UnitPrice.Should().Be(100m);
		nakitPrice.Currency.Should().Be("TRY");
		nakitPrice.ValidFrom.Should().Be(new DateTime(2025, 1, 1));
		nakitPrice.ValidTo.Should().Be(new DateTime(2025, 6, 30));
		nakitPrice.IsNew.Should().BeFalse("loaded prices should not be marked as IsNew");
		nakitPrice.IsModified.Should().BeFalse("loaded prices should not be marked as IsModified");

		var bayiPrice = vm.PriceList.First(p => p.ListCode == "BAYÝ");
		bayiPrice.UomName.Should().Be("BOX");
		bayiPrice.UnitPrice.Should().Be(95m);
		bayiPrice.Currency.Should().Be("USD");
		bayiPrice.ValidFrom.Should().BeNull();
		bayiPrice.ValidTo.Should().BeNull();
	}

	[Fact]
	public async Task SaveAsync_CallsAddPriceAsync_ForNewPrices()
	{
		// Arrange
		var product = new Product { Sku = "TEST-P7", Name = "Test Product 7", BaseUom = "EA", VatRate = 10, ReservedQty = 0 };
		Ctx.Products.Add(product);
		await Ctx.SaveChangesAsync();

		var mockPriceService = new Mock<IPriceListService>();
		mockPriceService.Setup(s => s.GetPricesByProductIdAsync(It.IsAny<int>()))
			.ReturnsAsync(new List<PriceDto>());
		mockPriceService.Setup(s => s.AddPriceAsync(It.IsAny<CreatePriceDto>()))
			.ReturnsAsync((CreatePriceDto dto) => new PriceDto(1, dto.ProductId, dto.ListCode, dto.UomName, dto.UnitPrice, dto.Currency, dto.ValidFrom, dto.ValidTo));

		var mockDialogService = new Mock<IDialogService>();

		var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, product.Id);
		await Task.Delay(100);

		// Add a new price
		vm.NewListCode = "NAKÝT";
		vm.NewPriceUomName = "EA";
		vm.NewUnitPrice = 100m;
		vm.NewCurrency = "TRY";
		vm.AddPriceCmd.Execute(null);

		vm.PriceList.Count.Should().Be(1);
		vm.PriceList[0].IsNew.Should().BeTrue();

		// Act - trigger SaveAsync
		vm.SaveCmd.Execute(null);
		await Task.Delay(200); // Wait for async save to complete

		// Assert
		mockPriceService.Verify(s => s.AddPriceAsync(It.Is<CreatePriceDto>(dto =>
			dto.ProductId == product.Id &&
			dto.ListCode == "NAKÝT" &&
			dto.UomName == "EA" &&
			dto.UnitPrice == 100m &&
			dto.Currency == "TRY"
		)), Times.Once, "AddPriceAsync should be called once for the new price");
	}

	[Fact]
	public async Task SaveAsync_CallsUpdatePriceAsync_ForModifiedPrices()
	{
		// Arrange
		var product = new Product { Sku = "TEST-P8", Name = "Test Product 8", BaseUom = "EA", VatRate = 10, ReservedQty = 0 };
		Ctx.Products.Add(product);
		await Ctx.SaveChangesAsync();

		var existingPrice = new PriceDto(1, product.Id, "VADELÝ", "EA", 110m, "TRY", null, null);

		var mockPriceService = new Mock<IPriceListService>();
		mockPriceService.Setup(s => s.GetPricesByProductIdAsync(product.Id))
			.ReturnsAsync(new List<PriceDto> { existingPrice });
		mockPriceService.Setup(s => s.UpdatePriceAsync(It.IsAny<int>(), It.IsAny<UpdatePriceDto>()))
			.ReturnsAsync((int id, UpdatePriceDto dto) => new PriceDto(id, product.Id, dto.ListCode, dto.UomName, dto.UnitPrice, dto.Currency, dto.ValidFrom, dto.ValidTo));

		var mockDialogService = new Mock<IDialogService>();

		var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, product.Id);
		await Task.Delay(100);

		vm.PriceList.Count.Should().Be(1);

		// Simulate user modifying the price by removing and re-adding with modified values
		// (In a real scenario, DataGrid would have edit mode, but we can test the save logic)
		var modifiedPrice = new PriceRow(
			existingPrice.Id,
			existingPrice.ListCode,
			existingPrice.UomName,
			120m, // Changed price
			existingPrice.Currency,
			existingPrice.ValidFrom,
			existingPrice.ValidTo,
			false, // IsNew = false
			true   // IsModified = true
		);

		vm.PriceList.Clear();
		vm.PriceList.Add(modifiedPrice);

		// Act
		vm.SaveCmd.Execute(null);
		await Task.Delay(200);

		// Assert
		mockPriceService.Verify(s => s.UpdatePriceAsync(existingPrice.Id, It.Is<UpdatePriceDto>(dto =>
			dto.Id == existingPrice.Id &&
			dto.UnitPrice == 120m
		)), Times.Once, "UpdatePriceAsync should be called once for the modified price");
	}

	[Fact]
	public async Task LoadPricesAsync_ReturnsEmptyCollection_ForNewProduct()
	{
		// Arrange - new product mode (no productId)
		var mockPriceService = new Mock<IPriceListService>();
		mockPriceService.Setup(s => s.GetPricesByProductIdAsync(It.IsAny<int>()))
			.ReturnsAsync(new List<PriceDto>());

		var mockDialogService = new Mock<IDialogService>();

		// Act
		var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, productId: null);
		await Task.Delay(100);

		// Assert
		vm.PriceList.Count.Should().Be(0, "new products should have empty price list");
		mockPriceService.Verify(s => s.GetPricesByProductIdAsync(It.IsAny<int>()), Times.Never, 
			"service should not be called for new products");
	}

	[Fact]
	public async Task SaveAsync_SavesMultipleNewPrices_InSingleOperation()
	{
		// Arrange
		var product = new Product { Sku = "TEST-P9", Name = "Test Product 9", BaseUom = "EA", VatRate = 10, ReservedQty = 0 };
		Ctx.Products.Add(product);
		await Ctx.SaveChangesAsync();

		var mockPriceService = new Mock<IPriceListService>();
		mockPriceService.Setup(s => s.GetPricesByProductIdAsync(It.IsAny<int>()))
			.ReturnsAsync(new List<PriceDto>());
		mockPriceService.Setup(s => s.AddPriceAsync(It.IsAny<CreatePriceDto>()))
			.ReturnsAsync((CreatePriceDto dto) => new PriceDto(1, dto.ProductId, dto.ListCode, dto.UomName, dto.UnitPrice, dto.Currency, dto.ValidFrom, dto.ValidTo));

		var mockDialogService = new Mock<IDialogService>();

		var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, product.Id);
		await Task.Delay(100);

		// Add 3 prices (acceptance criteria: NAKÝT, VADELÝ, BAYÝ)
		vm.NewListCode = "NAKÝT";
		vm.NewPriceUomName = "EA";
		vm.NewUnitPrice = 100m;
		vm.NewCurrency = "TRY";
		vm.AddPriceCmd.Execute(null);

		vm.NewListCode = "VADELÝ";
		vm.NewPriceUomName = "EA";
		vm.NewUnitPrice = 110m;
		vm.NewCurrency = "TRY";
		vm.AddPriceCmd.Execute(null);

		vm.NewListCode = "BAYÝ";
		vm.NewPriceUomName = "EA";
		vm.NewUnitPrice = 95m;
		vm.NewCurrency = "USD";
		vm.AddPriceCmd.Execute(null);

		vm.PriceList.Count.Should().Be(3, "three prices should be added");

		// Act
		vm.SaveCmd.Execute(null);
		await Task.Delay(300);

		// Assert
		mockPriceService.Verify(s => s.AddPriceAsync(It.IsAny<CreatePriceDto>()), Times.Exactly(3),
			"AddPriceAsync should be called 3 times for the 3 new prices");
	}
}

using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Tests.Infrastructure;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Presentation.Abstractions;
using InventoryERP.Application.Products;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;

namespace Tests.Unit;

/// <summary>
/// R-051: UOM ComboBox Feature - Replace TextBox with dropdown using Turkish UOM names
/// </summary>
public class R051_BaseUomDropdownTests : BaseIntegrationTest
{
    [Fact]
    public void R051_ItemEditViewModel_BaseUomOptions_IsPopulated_FromUnitOfMeasure()
    {
        // Arrange
        var mockPriceService = new Mock<IPriceListService>();
        mockPriceService.Setup(s => s.GetPricesByProductIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new System.Collections.Generic.List<PriceDto>());

        var mockDialogService = new Mock<IDialogService>();

        // Act: Create new ItemEditViewModel (for new product)
        var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, productId: null);

        // Assert: BaseUomOptions should be populated from UnitOfMeasure.GetAllCodes()
        vm.BaseUomOptions.Should().NotBeNull("BaseUomOptions should be initialized");
        vm.BaseUomOptions.Should().NotBeEmpty("BaseUomOptions should contain UOM codes");
        
        var expectedCodes = UnitOfMeasure.GetAllCodes();
        vm.BaseUomOptions.Should().BeEquivalentTo(expectedCodes, 
            "BaseUomOptions should match UnitOfMeasure.GetAllCodes()");
    }

    [Fact]
    public void R051_BaseUomOptions_ContainsTurkishFriendlyCodes()
    {
        // Arrange
        var mockPriceService = new Mock<IPriceListService>();
        mockPriceService.Setup(s => s.GetPricesByProductIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new System.Collections.Generic.List<PriceDto>());

        var mockDialogService = new Mock<IDialogService>();

        var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, productId: null);

        // Assert: Check for expected Turkish UOM codes
        vm.BaseUomOptions.Should().Contain(UnitOfMeasure.Adet, "ADET should be available");
        vm.BaseUomOptions.Should().Contain(UnitOfMeasure.Kilogram, "KG should be available");
        vm.BaseUomOptions.Should().Contain(UnitOfMeasure.Koli, "KOLI should be available");
        vm.BaseUomOptions.Should().Contain(UnitOfMeasure.Set, "SET should be available");
        vm.BaseUomOptions.Should().Contain(UnitOfMeasure.Düzine, "DUZINE should be available");
    }

    [Fact]
    public async Task R051_BaseUomOptions_IsAvailable_ForExistingProducts()
    {
        // Arrange: Create existing product
        var product = new Product 
        { 
            Sku = "TEST-R051", 
            Name = "Test Product R051", 
            BaseUom = "KG", 
            VatRate = 20, 
            ReservedQty = 0 
        };
        Ctx.Products.Add(product);
        await Ctx.SaveChangesAsync();

        var mockPriceService = new Mock<IPriceListService>();
        mockPriceService.Setup(s => s.GetPricesByProductIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new System.Collections.Generic.List<PriceDto>());

        var mockDialogService = new Mock<IDialogService>();

        // Act: Load existing product
        var vm = new ItemEditViewModel(Ctx, mockPriceService.Object, mockDialogService.Object, product.Id);
        await Task.Delay(100); // Wait for LoadAsync to complete

        // Assert: BaseUomOptions should still be available for editing
        vm.BaseUomOptions.Should().NotBeEmpty("BaseUomOptions should be available when editing");
        vm.BaseUom.Should().Be("KG", "BaseUom should be loaded from existing product");
        vm.BaseUomOptions.Should().Contain("KG", "Current UOM should be in the dropdown");
    }
}

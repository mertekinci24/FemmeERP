using System;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Xunit;

namespace Tests.Unit;

public class PartnerValidationTests
{
    [Fact]
    public void Validate_Allows_VKN_When_TCKN_Invalid_Or_Empty()
    {
        // UAT Row 2: SUPPLIER with 10-digit VKN and empty TCKN should pass
        var p1 = new Partner
        {
            PartnerType = PartnerType.Supplier,
            Name = "Rosei Rose",
            TaxId = "2222222222", // valid VKN (10 digits)
            NationalId = null      // empty TCKN
        };

        var ex1 = Record.Exception(() => p1.Validate());
        Assert.Null(ex1);

        // Also allow when TCKN is present but invalid while VKN is valid
        var p2 = new Partner
        {
            PartnerType = PartnerType.Supplier,
            Name = "Acme",
            TaxId = "1234567890", // valid VKN
            NationalId = "123"      // invalid TCKN
        };

        var ex2 = Record.Exception(() => p2.Validate());
        Assert.Null(ex2);
    }

    [Fact]
    public void Validate_Allows_TCKN_When_VKN_Invalid_Or_Empty()
    {
        var p1 = new Partner
        {
            PartnerType = PartnerType.Customer,
            Name = "John Doe",
            TaxId = null,
            NationalId = "12345678901" // valid TCKN (11 digits)
        };

        var ex1 = Record.Exception(() => p1.Validate());
        Assert.Null(ex1);

        var p2 = new Partner
        {
            PartnerType = PartnerType.Customer,
            Name = "Jane",
            TaxId = "123",            // invalid VKN
            NationalId = "12345678901" // valid TCKN
        };

        var ex2 = Record.Exception(() => p2.Validate());
        Assert.Null(ex2);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData("123", null)]
    [InlineData(null, "123")]
    public void Validate_Throws_When_Neither_Identifier_Is_Valid(string? taxId, string? nationalId)
    {
        var p = new Partner
        {
            PartnerType = PartnerType.Customer,
            Name = "X",
            TaxId = taxId,
            NationalId = nationalId
        };

        Assert.ThrowsAny<InvalidOperationException>(() => p.Validate());
    }
}

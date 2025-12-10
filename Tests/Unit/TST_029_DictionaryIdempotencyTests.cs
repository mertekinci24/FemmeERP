using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Unit;

/// <summary>
/// TST-029: Ensure repeated loads in DocumentEditViewModel do not throw
/// System.ArgumentException (same key) when opening Sales Order twice.
/// </summary>
public class TST_029_DictionaryIdempotencyTests
{
    [Fact]
    public async Task LoadPartners_Twice_Does_Not_Throw_For_SalesOrder()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var dto = new InventoryERP.Application.Documents.DTOs.DocumentDetailDto
            {
                Id = 0,
                Type = "SALES_ORDER",
                Date = DateTime.Today,
                Lines = new System.Collections.Generic.List<InventoryERP.Application.Documents.DTOs.DocumentLineDto>()
            };

            var vm = ActivatorUtilities.CreateInstance<InventoryERP.Presentation.ViewModels.DocumentEditViewModel>(provider, dto);

            // Use reflection to invoke private LoadPartnersAsync twice
            var mi = typeof(InventoryERP.Presentation.ViewModels.DocumentEditViewModel).GetMethod("LoadPartnersAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            mi.Should().NotBeNull();

            var t1 = (Task)mi!.Invoke(vm, Array.Empty<object>());
            await t1;
            var t2 = (Task)mi!.Invoke(vm, Array.Empty<object>());
            await t2;
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task LoadProducts_Twice_Does_Not_Throw()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var dto = new InventoryERP.Application.Documents.DTOs.DocumentDetailDto
            {
                Id = 0,
                Type = "SALES_ORDER",
                Date = DateTime.Today,
                Lines = new System.Collections.Generic.List<InventoryERP.Application.Documents.DTOs.DocumentLineDto>()
            };

            var vm = ActivatorUtilities.CreateInstance<InventoryERP.Presentation.ViewModels.DocumentEditViewModel>(provider, dto);

            var mi = typeof(InventoryERP.Presentation.ViewModels.DocumentEditViewModel).GetMethod("LoadProductsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            mi.Should().NotBeNull();
            var t1 = (Task)mi!.Invoke(vm, Array.Empty<object>());
            await t1;
            var t2 = (Task)mi!.Invoke(vm, Array.Empty<object>());
            await t2;
        }
        finally { conn.Dispose(); }
    }
}


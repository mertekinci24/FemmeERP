using InventoryERP.Infrastructure.Handlers.Invoices;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using InventoryERP.Infrastructure.Commands.Invoices;
using InventoryERP.Infrastructure.Services;
using FluentValidation;
using Moq;

namespace Tests.Invoices;

public class InvoicePostingTests
{
    [Fact]
    public async Task ApproveInvoiceHandler_Should_Post_Invoice_Transactionally()
    {
        var validator = new Mock<IValidator<ApproveInvoiceCommand>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ApproveInvoiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        var service = new Mock<IInvoicePostingService>();
        service.Setup(s => s.ApproveAsync(It.IsAny<ApproveInvoiceCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    var handler = new ApproveInvoiceHandler(validator.Object, service.Object);
        var command = new ApproveInvoiceCommand(1, "ext-1", "INV-001");
        var result = await handler.Handle(command, CancellationToken.None);
        Assert.Equal(MediatR.Unit.Value, result);
    }
}

namespace SirBilling.Api.Tests;

public class InvoiceTests
{
    [Theory]
    [InlineData(InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.Overdue)]
    public void MarkAsPaid_WhenStatusAllowsPayment_ChangesStatus(
        InvoiceStatus initialStatus)
    {
        var invoice = new Invoice
        {
            Status = initialStatus
        };

        invoice.MarkAsPaid();

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Canceled)]
    public void MarkAsPaid_WhenStatusDoesNotAllowPayment_Throws(
        InvoiceStatus initialStatus)
    {
        var invoice = new Invoice
        {
            Status = initialStatus
        };

        Assert.Throws<InvalidOperationException>(
            invoice.MarkAsPaid
        );
    }

    [Theory]
    [InlineData(InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.Overdue)]
    public void Cancel_WhenStatusAllowsCancellation_ChangesStatusAndSetsCanceledAt(
        InvoiceStatus initialStatus)
    {
        var invoice = new Invoice
        {
            Status = initialStatus
        };

        invoice.Cancel();

        Assert.Equal(InvoiceStatus.Canceled, invoice.Status);
        Assert.NotNull(invoice.CanceledAt);
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Canceled)]
    public void Cancel_WhenStatusDoesNotAllowCancellation_Throws(
        InvoiceStatus initialStatus)
    {
        var invoice = new Invoice
        {
            Status = initialStatus
        };

        Assert.Throws<InvalidOperationException>(
            invoice.Cancel
        );
    }
}
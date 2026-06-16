namespace FreelanceManager.Core.Services;

public interface IInvoiceNumberGenerator
{
    string Next(string format, int year, int lastSequenceThisYear);
}

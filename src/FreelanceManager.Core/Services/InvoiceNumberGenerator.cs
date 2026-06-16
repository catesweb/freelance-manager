using System.Text.RegularExpressions;

namespace FreelanceManager.Core.Services;

public sealed class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    public string Next(string format, int year, int lastSequenceThisYear)
    {
        int seq = lastSequenceThisYear + 1;
        string result = format.Replace("{YYYY}", year.ToString("D4"));

        result = Regex.Replace(result, @"\{(0+)\}", m =>
        {
            int width = m.Groups[1].Value.Length;
            return seq.ToString(new string('0', width));
        });

        return result;
    }
}

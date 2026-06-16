using FreelanceManager.Core.Services;
using Xunit;

namespace FreelanceManager.Tests;

public class InvoiceNumberGeneratorTests
{
    private readonly InvoiceNumberGenerator _gen = new();

    [Fact]
    public void First_number_of_year_uses_sequence_one()
        => Assert.Equal("INV-2026-0001", _gen.Next("INV-{YYYY}-{0000}", 2026, lastSequenceThisYear: 0));

    [Fact]
    public void Increments_from_last_sequence()
        => Assert.Equal("INV-2026-0008", _gen.Next("INV-{YYYY}-{0000}", 2026, lastSequenceThisYear: 7));

    [Fact]
    public void Pads_to_width_of_zero_run()
        => Assert.Equal("INV-2026-042", _gen.Next("INV-{YYYY}-{000}", 2026, lastSequenceThisYear: 41));

    [Fact]
    public void Sequence_wider_than_padding_is_not_truncated()
        => Assert.Equal("INV-2026-1000", _gen.Next("INV-{YYYY}-{000}", 2026, lastSequenceThisYear: 999));

    [Fact]
    public void Year_token_is_substituted()
        => Assert.Equal("2027/0001", _gen.Next("{YYYY}/{0000}", 2027, lastSequenceThisYear: 0));
}

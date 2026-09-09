using System.Globalization;
using System.Windows.Controls;
using Financial.Presentation.App.Input;
using FluentAssertions;

namespace Financial.Presentation.Tests.Input;

public class DecimalInputHelperTests
{
    [Fact]
    public void IsDecimalTextAllowed_ValidProposedText_ReturnsTrue()
    {
        RunOnStaThread(() =>
        {
            var textBox = new TextBox { Text = "10" };
            textBox.Select(2, 0);

            DecimalInputHelper.IsDecimalTextAllowed(textBox, "5").Should().BeTrue();
        });
    }

    [Fact]
    public void IsDecimalTextAllowed_InvalidProposedText_ReturnsFalse()
    {
        RunOnStaThread(() =>
        {
            var textBox = new TextBox { Text = "10.5" };
            textBox.Select(4, 0);

            DecimalInputHelper.IsDecimalTextAllowed(textBox, ".5").Should().BeFalse();
        });
    }

    [Fact]
    public void IsSignedDecimalTextAllowed_ValidProposedText_ReturnsTrue()
    {
        RunOnStaThread(() =>
        {
            var textBox = new TextBox { Text = "" };
            textBox.Select(0, 0);

            DecimalInputHelper.IsSignedDecimalTextAllowed(textBox, "-10").Should().BeTrue();
        });
    }

    [Fact]
    public void IsSignedDecimalTextAllowed_InvalidProposedText_ReturnsFalse()
    {
        RunOnStaThread(() =>
        {
            var textBox = new TextBox { Text = "10" };
            textBox.Select(2, 0);

            DecimalInputHelper.IsSignedDecimalTextAllowed(textBox, "-").Should().BeFalse();
        });
    }

    [Fact]
    public void GetProposedText_WithSelectedText_RemovesItBeforeInserting()
    {
        RunOnStaThread(() =>
        {
            var textBox = new TextBox { Text = "12345" };
            textBox.Select(1, 3);

            DecimalInputHelper.GetProposedText(textBox, "9").Should().Be("195");
        });
    }

    [Fact]
    public void GetProposedText_WithoutSelection_InsertsAtCaret()
    {
        RunOnStaThread(() =>
        {
            var textBox = new TextBox { Text = "12" };
            textBox.Select(1, 0);

            DecimalInputHelper.GetProposedText(textBox, "9").Should().Be("192");
        });
    }

    /// <summary>WPF's TextBox requires STA thread affinity (InputManager/KeyboardNavigation), which
    /// the default xUnit test thread does not provide.</summary>
    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure != null)
        {
            throw failure;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("10")]
    [InlineData("10.5")]
    [InlineData("10,5")]
    public void IsValidDecimalInput_UnsignedText_ReturnsTrue(string text)
    {
        DecimalInputHelper.IsValidDecimalInput(text).Should().BeTrue();
    }

    [Theory]
    [InlineData("-")]
    [InlineData("-10")]
    [InlineData("-10.5")]
    public void IsValidDecimalInput_NegativeText_ReturnsFalse(string text)
    {
        DecimalInputHelper.IsValidDecimalInput(text).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("10")]
    [InlineData("10.5")]
    [InlineData("-10")]
    [InlineData("-10.5")]
    [InlineData("-10,5")]
    public void IsValidSignedDecimalInput_LeadingMinusOrNone_ReturnsTrue(string text)
    {
        DecimalInputHelper.IsValidSignedDecimalInput(text).Should().BeTrue();
    }

    [Theory]
    [InlineData("1-0")]
    [InlineData("10-")]
    [InlineData("--10")]
    [InlineData("10.5.5")]
    public void IsValidSignedDecimalInput_MisplacedOrRepeatedSigns_ReturnsFalse(string text)
    {
        DecimalInputHelper.IsValidSignedDecimalInput(text).Should().BeFalse();
    }

    [Fact]
    public void NormalizeDecimalSeparator_DotCulture_ReplacesCommaWithDot()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US"); // NumberDecimalSeparator: "."

            DecimalInputHelper.NormalizeDecimalSeparator("10,5").Should().Be("10.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void NormalizeDecimalSeparator_CommaCulture_ReplacesDotWithComma()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR"); // NumberDecimalSeparator: ","

            DecimalInputHelper.NormalizeDecimalSeparator("10.5").Should().Be("10,5");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}

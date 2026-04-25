using CHAP2.Application.Helpers;
using FluentAssertions;

namespace CHAP2.Tests.Application;

[TestFixture]
public class BibleTextNormalizerTests
{
    [TestCase("Wêreld", "wereld")]
    [TestCase("Esegiël", "esegiel")]
    [TestCase("Daniël", "daniel")]
    [TestCase("Joël", "joel")]
    [TestCase("1 Korintiërs", "1korintiers")]
    [TestCase("  Joh   3:16  ", "joh316")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void Identifier_StripsDiacriticsAndPunctuation(string? input, string expected)
    {
        BibleTextNormalizer.Identifier(input).Should().Be(expected);
    }

    [TestCase("Wêreld", "wereld")]
    [TestCase("In die begin het God", "in die begin het god")]
    [TestCase("Want so lief het God die wêreld gehad,", "want so lief het god die wereld gehad")]
    [TestCase("  een   geloof,  een doop ", "een geloof een doop")]
    [TestCase("multiple\nlines\twith   spaces", "multiple lines with spaces")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void SearchableText_LowercasesStripsDiacriticsCollapsesWhitespace(string? input, string expected)
    {
        BibleTextNormalizer.SearchableText(input).Should().Be(expected);
    }

    [Test]
    public void WordsFor_SplitsOnWhitespaceAfterNormalization()
    {
        BibleTextNormalizer.WordsFor("een geloof, een doop").Should().Equal("een", "geloof", "een", "doop");
        BibleTextNormalizer.WordsFor("  Wêreld  ").Should().Equal("wereld");
        BibleTextNormalizer.WordsFor("").Should().BeEmpty();
        BibleTextNormalizer.WordsFor(null).Should().BeEmpty();
    }
}

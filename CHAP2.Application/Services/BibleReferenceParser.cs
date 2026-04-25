using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.ValueObjects;

namespace CHAP2.Application.Services;

public class BibleReferenceParser : IBibleReferenceParser
{
    // Matches: optional book number + book name (letters), optional chapter, optional ":verse".
    // Examples it accepts: "Joh 3:16", "1 Kor 13", "psalms23:1", "1joh4:8", "matteus".
    private static readonly Regex ReferenceShape = new(
        @"^\s*(?<book>(?:\d+\s*)?[a-zA-Z]+)\s*(?:(?<chapter>\d+)(?:\s*[:.]\s*(?<verse>\d+))?)?\s*$",
        RegexOptions.Compiled);

    private static readonly Dictionary<string, string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Old Testament shortcuts
        ["gen"] = "genesis", ["ex"] = "eksodus", ["eks"] = "eksodus", ["lev"] = "levitikus",
        ["num"] = "numeri", ["deut"] = "deuteronomium", ["jos"] = "josua", ["rig"] = "rigters",
        ["1sam"] = "1-samuel", ["2sam"] = "2-samuel", ["1kon"] = "1-konings", ["2kon"] = "2-konings",
        ["1kron"] = "1-kronieke", ["2kron"] = "2-kronieke",
        ["neh"] = "nehemia", ["est"] = "ester", ["ps"] = "psalms", ["psalm"] = "psalms",
        ["spr"] = "spreuke", ["pred"] = "prediker", ["hoog"] = "hooglied",
        ["jes"] = "jesaja", ["jer"] = "jeremia", ["klaag"] = "klaagliedere",
        ["eseg"] = "esegiel", ["dan"] = "daniel",
        ["hos"] = "hosea", ["amos"] = "amos", ["obad"] = "obadja", ["jona"] = "jona",
        ["miga"] = "miga", ["nah"] = "nahum", ["hab"] = "habakuk", ["sef"] = "sefanja",
        ["hag"] = "haggai", ["sag"] = "sagaria", ["mal"] = "maleagi",
        // New Testament shortcuts
        ["mat"] = "matteus", ["matt"] = "matteus", ["mark"] = "markus", ["luk"] = "lukas",
        ["joh"] = "johannes", ["hand"] = "handelinge", ["rom"] = "romeine",
        ["1kor"] = "1-korintiers", ["2kor"] = "2-korintiers",
        ["gal"] = "galasiers", ["ef"] = "efesiers", ["fil"] = "filippense",
        ["kol"] = "kolossense", ["1tess"] = "1-tessalonisense", ["2tess"] = "2-tessalonisense",
        ["1tim"] = "1-timoteus", ["2tim"] = "2-timoteus", ["tit"] = "titus",
        ["filem"] = "filemon", ["heb"] = "hebreers", ["jak"] = "jakobus",
        ["1pet"] = "1-petrus", ["2pet"] = "2-petrus",
        ["1joh"] = "1-johannes", ["2joh"] = "2-johannes", ["3joh"] = "3-johannes",
        ["jud"] = "judas", ["op"] = "openbaring", ["openb"] = "openbaring",
    };

    private readonly IBibleBookRepository _bookRepository;

    public BibleReferenceParser(IBibleBookRepository bookRepository)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
    }

    public async Task<BibleReference?> TryParseAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var match = ReferenceShape.Match(input);
        if (!match.Success)
            return null;

        var bookFragment = match.Groups["book"].Value;
        var chapterText = match.Groups["chapter"].Value;
        var verseText = match.Groups["verse"].Value;

        var bookId = await ResolveBookIdAsync(bookFragment, cancellationToken);
        if (bookId is null)
            return null;

        var chapter = string.IsNullOrEmpty(chapterText) ? 1 : int.Parse(chapterText, CultureInfo.InvariantCulture);
        int? verse = string.IsNullOrEmpty(verseText) ? null : int.Parse(verseText, CultureInfo.InvariantCulture);

        return new BibleReference(bookId, chapter, verse);
    }

    private async Task<string?> ResolveBookIdAsync(string fragment, CancellationToken cancellationToken)
    {
        var normalized = Normalize(fragment);
        if (normalized.Length == 0)
            return null;

        if (Abbreviations.TryGetValue(normalized, out var abbreviated))
            return abbreviated;

        var books = await _bookRepository.GetAllBooksAsync(cancellationToken);

        foreach (var book in books)
        {
            if (Normalize(book.Id) == normalized) return book.Id;
            if (Normalize(book.Name) == normalized) return book.Name == book.Id ? book.Id : book.Id;
        }

        BibleBook? prefixHit = null;
        var prefixCount = 0;
        foreach (var book in books)
        {
            var bookKey = Normalize(book.Name);
            if (bookKey.StartsWith(normalized, StringComparison.Ordinal))
            {
                prefixHit = book;
                prefixCount++;
                if (prefixCount > 1) break;
            }
        }
        return prefixCount == 1 ? prefixHit!.Id : null;
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }
}

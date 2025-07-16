namespace CHAP2.Console.Common.Configuration;

public class ConsoleDisplaySettings
{
    public int HeaderLines { get; set; } = 3; // Title + separator + empty line
    public int SearchPromptLines { get; set; } = 2; // "Search: " + empty line
    public int InstructionLines { get; set; } = 2; // Instructions or status messages
    public int SafetyMargin { get; set; } = 1;
    public int MinResultsToShow { get; set; } = 1;
    public int SearchPromptPrefixLength { get; set; } = 8; // "Search: " length
    public int ResultPrefixLength { get; set; } = 4; // "1. " prefix length
    public int ContentPrefixLength { get; set; } = 8; // "    Text: " prefix length
    public int TruncationSuffixLength { get; set; } = 3; // "..." length
    public string TruncationSuffix { get; set; } = "...";
    public string SearchPromptText { get; set; } = "Search: ";
    public string HeaderTitle { get; set; } = "CHAP2 Search Console - Interactive Search Mode";
    public string HeaderSeparator { get; set; } = "=============================================";
    public string InstructionsText { get; set; } = "Type to search choruses. Search triggers after each keystroke with delay.";
    public string InstructionsControls { get; set; } = "Press Enter to select, Escape to clear search, Ctrl+C to exit.";
    public string NoResultsText { get; set; } = "No results found.";
    public string MoreResultsText { get; set; } = "... and {0} more results";
    public string ShortSearchText { get; set; } = "Type at least {0} characters to search...";
    
    // Column display settings
    public int MinTitleColumnWidth { get; set; } = 30;
    public int MinKeyColumnWidth { get; set; } = 8; // Width for key column
    public int TotalContextCharacters { get; set; } = 20; // Total characters around search term
    public string TitleColumnHeader { get; set; } = "Title";
    public string KeyColumnHeader { get; set; } = "Key";
    public string ContextColumnHeader { get; set; } = "Context";
    public string ColumnSeparator { get; set; } = "  ";
    public string TitlePrefix { get; set; } = "Title: ";
    public string TextPrefix { get; set; } = "Text: ";
    public string LeftEllipsis { get; set; } = "...";
    public string RightEllipsis { get; set; } = "...";
} 
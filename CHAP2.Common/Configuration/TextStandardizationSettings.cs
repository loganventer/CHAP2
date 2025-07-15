namespace CHAP2.Common.Configuration;

public class TextStandardizationSettings
{
    public string[] ReligiousTitles { get; set; } = {
        // English titles
        "God", "Heer", "Jesus", "Christ", "Lord", "Savior", "Holy", "Spirit",
        "Father", "Son", "Almighty", "Creator", "Redeemer", "Messiah",
        // Afrikaans titles
        "Here", "Vader", "Seun", "Heilige", "Geest", "Verlosser", "Messias",
        "Almagtige", "Skepper", "Verlosser", "Koning", "Koningin", "Profete",
        "Apostel", "Priester", "Pastor", "Predikant", "Biskop", "Kardinaal",
        "Pous", "Pater", "Broeder", "Suster", "Non", "Monnik", "Kluisenaar"
    };
    
    public int CacheDurationMinutes { get; set; } = 10;
    public int ConnectivityTestTimeoutSeconds { get; set; } = 10;
    public int MaxSearchTermLength { get; set; } = 50;
    public int MaxContentLength { get; set; } = 100;
    public int SafetyMargin { get; set; } = 1;
    public int MinResultsToShow { get; set; } = 1;
} 
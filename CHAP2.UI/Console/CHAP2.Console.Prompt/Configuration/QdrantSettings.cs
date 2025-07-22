namespace CHAP2.Console.Prompt.Configuration;

public class QdrantSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6333;
    public string CollectionName { get; set; } = "chorus-vectors";
    public int VectorSize { get; set; } = 1536;
} 
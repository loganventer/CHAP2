namespace CHAP2.WebPortal.Configuration;

public class LangChainSettings
{
    public string BaseUrl { get; set; } = "http://langchain-service:8000";
    public int TimeoutSeconds { get; set; } = 600;
} 
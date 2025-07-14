namespace CHAP2API.Configuration;

public class ApiSettings
{
    public string GlobalRoutePrefix { get; set; } = "api";
    public int DefaultPort { get; set; } = 5000;
    public string MaxRequestSize { get; set; } = "10MB";
} 
namespace CHAP2.Console.Vectorize.Services;

public interface IVectorizationOrchestrator
{
    Task VectorizeChorusDataAsync(string dataPath);
} 
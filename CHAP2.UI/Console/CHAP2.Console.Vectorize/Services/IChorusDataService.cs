using CHAP2.Console.Vectorize.DTOs;

namespace CHAP2.Console.Vectorize.Services;

public interface IChorusDataService
{
    Task<List<ChorusDataDto>> LoadChorusDataAsync(string dataPath);
    Task<List<string>> GetChorusFilePathsAsync(string dataPath);
} 
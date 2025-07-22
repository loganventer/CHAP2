using CHAP2.Console.Prompt.DTOs;

namespace CHAP2.Console.Prompt.Services;

public interface IPromptService
{
    Task<string> AskQuestionAsync(string question);
    Task<List<ChorusSearchResult>> SearchChorusesAsync(string query);
} 
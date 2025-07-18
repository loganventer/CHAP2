using CHAP2.Domain.Entities;
using CHAP2.Shared.ViewModels;

namespace CHAP2.Application.Interfaces;

public interface IChorusApplicationService
{
    Task<bool> CreateChorusAsync(ChorusCreateViewModel model);
    Task<bool> UpdateChorusAsync(ChorusEditViewModel model);
    Task<bool> DeleteChorusAsync(string id);
    Task<Chorus?> GetChorusByIdAsync(string id);
    Task<IEnumerable<Chorus>> SearchChorusesAsync(string query, string searchMode, string searchIn);
} 
using CHAP2.Domain.Entities;

namespace CHAP2.Console.Common.Interfaces;

public interface ISelectionService
{
    int SelectedIndex { get; set; }
    int TotalItems { get; set; }
    bool IsInDetailView { get; set; }
    void MoveUp();
    void MoveDown();
    void SelectCurrent();
    void ResetSelection();
    void UpdateTotalItems(int totalItems);
    bool TrySelectByNumber(int number);
} 
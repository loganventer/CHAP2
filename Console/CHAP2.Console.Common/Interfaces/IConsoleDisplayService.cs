using CHAP2.Common.Models;

namespace CHAP2.Console.Common.Interfaces;

public interface IConsoleDisplayService
{
    void DisplayChorus(Chorus chorus);
    void DisplayChoruses(List<Chorus> choruses);
    void DisplayChorusDetail(Chorus chorus);
    void ClearScreen();
    void SetCursorPosition(int left, int top);
    void WriteLine(string text);
    void Write(string text);
} 
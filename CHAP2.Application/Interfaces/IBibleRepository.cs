namespace CHAP2.Application.Interfaces;

/// <summary>
/// Composite repository interface for Bible data following IDesign / ISP principles.
/// Inherits the segregated interfaces so consumers can depend on the narrowest contract they need.
/// </summary>
public interface IBibleRepository : IBibleBookRepository, IBibleChapterRepository, IBibleVerseSearchRepository
{
}

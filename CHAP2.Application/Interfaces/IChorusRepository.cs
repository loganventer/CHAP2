namespace CHAP2.Application.Interfaces;

/// <summary>
/// Composite repository interface for Chorus entities following IDesign principles.
/// Inherits from segregated interfaces for Interface Segregation Principle compliance.
/// Use specific interfaces (IChorusReadRepository, IChorusWriteRepository, IChorusSearchRepository)
/// when only specific operations are needed.
/// </summary>
public interface IChorusRepository : IChorusReadRepository, IChorusWriteRepository, IChorusSearchRepository
{
}

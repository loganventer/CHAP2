using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

/// <summary>
/// Interface for converting PowerPoint slides to chorus entities
/// </summary>
public interface ISlideToChorusService
{
    /// <summary>
    /// Convert PowerPoint slide file content to a chorus entity
    /// </summary>
    /// <param name="fileContent">Raw file content as byte array</param>
    /// <param name="filename">Original filename for reference</param>
    /// <returns>Chorus entity created from the slide content</returns>
    Chorus ConvertToChorus(byte[] fileContent, string filename);
} 
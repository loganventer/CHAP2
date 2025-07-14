using CHAP2.Common.Models;

namespace CHAP2.Common.Interfaces
{
    public interface ISlideToChorusService
    {
        Chorus ConvertToChorus(byte[] fileContent, string filename);
    }
} 
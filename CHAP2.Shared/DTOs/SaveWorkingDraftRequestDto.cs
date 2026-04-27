namespace CHAP2.Shared.DTOs;

public class SaveWorkingDraftRequestDto
{
    public IReadOnlyList<SetlistItemPayloadDto> Items { get; set; } = Array.Empty<SetlistItemPayloadDto>();
}

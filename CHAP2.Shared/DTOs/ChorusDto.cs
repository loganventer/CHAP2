using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class ChorusDto
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string ChorusText { get; set; } = string.Empty;
    
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TimeSignature { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ChorusMetadataDto? Metadata { get; set; }
}

public class CreateChorusDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string ChorusText { get; set; } = string.Empty;
    
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TimeSignature { get; set; } = string.Empty;
    public ChorusMetadataDto? Metadata { get; set; }
}

public class UpdateChorusDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string ChorusText { get; set; } = string.Empty;
    
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TimeSignature { get; set; } = string.Empty;
    public ChorusMetadataDto? Metadata { get; set; }
}

public class ChorusMetadataDto
{
    public string? Composer { get; set; }
    public string? Arranger { get; set; }
    public string? Copyright { get; set; }
    public string? Language { get; set; }
    public string? Genre { get; set; }
    public int? Tempo { get; set; }
    public string? Difficulty { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

public class SearchRequestDto
{
    [Required]
    public string Query { get; set; } = string.Empty;
    public string SearchMode { get; set; } = "Contains";
    public string SearchIn { get; set; } = "all";
}

public class SearchResponseDto
{
    public string Query { get; set; } = string.Empty;
    public string SearchMode { get; set; } = string.Empty;
    public string SearchIn { get; set; } = string.Empty;
    public int Count { get; set; }
    public int MaxResults { get; set; }
    public List<ChorusDto> Results { get; set; } = new();
} 
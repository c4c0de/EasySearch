namespace InventoryManagement.Application.DTOs;

public class ContentPageDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class BranchDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Hours { get; set; } = string.Empty;
    public string? MapsUrl { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

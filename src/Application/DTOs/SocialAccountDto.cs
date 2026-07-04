using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.DTOs;

public class SocialAccountDto
{
    public Guid Id { get; set; }
    public SocialAccountType Type { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public string Display => string.IsNullOrWhiteSpace(Label) ? Value : $"{Label} ({Value})";
}

public class CreateSocialAccountDto
{
    public Guid? Id { get; set; }
    public SocialAccountType Type { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

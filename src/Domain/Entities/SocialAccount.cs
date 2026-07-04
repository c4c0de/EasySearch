using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

public class SocialAccount
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public Dealer Dealer { get; set; } = null!;

    public SocialAccountType Type { get; set; }
    public string Label { get; set; } = string.Empty;   // e.g. "Sales line", "Main IG"
    public string Value { get; set; } = string.Empty;    // WhatsApp number (digits + country code) or IG handle
    public bool IsDefault { get; set; }
}

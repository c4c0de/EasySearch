namespace InventoryManagement.Web;

/// <summary>App-wide constants. Single-dealer deployment — one seeded dealer id used everywhere.</summary>
public static class AppConstants
{
    public static readonly Guid DefaultDealerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
}

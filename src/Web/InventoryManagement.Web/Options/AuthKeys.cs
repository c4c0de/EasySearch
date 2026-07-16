namespace InventoryManagement.Web.Options;

/// <summary>
/// SiteSettings keys for the admin credential. Deliberately NOT part of SettingKeys.All —
/// these must never appear on the Site Settings page, in SiteConfig snapshots, or as
/// {{tokens}} in rendered pages.
/// </summary>
public static class AuthKeys
{
    public const string Username = "Auth.Username";
    public const string PasswordHash = "Auth.PasswordHash";

    /// <summary>Prefix used to exclude credential keys from public-facing config snapshots.</summary>
    public const string Prefix = "Auth.";
}

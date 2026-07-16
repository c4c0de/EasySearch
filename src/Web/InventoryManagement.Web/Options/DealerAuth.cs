namespace InventoryManagement.Web.Options;

/// <summary>
/// FIRST-BOOT SEED ONLY. The live credential is stored (hashed) in the SiteSettings table and
/// managed at /account/change-password. These config values are copied into the DB the first
/// time the app runs against an empty credential and are ignored afterwards.
/// </summary>
public class DealerAuth
{
    public const string SectionName = "DealerAuth";

    public string Username { get; set; } = "admin";

    /// <summary>Plaintext fallback — only used when PasswordHash is empty. Prefer PasswordHash.</summary>
    public string Password { get; set; } = "change-me";

    /// <summary>PBKDF2 hash ("iterations.saltB64.hashB64"). When set, it takes precedence over Password.</summary>
    public string PasswordHash { get; set; } = "";
}

namespace InventoryManagement.Web.Options;

/// <summary>
/// Single dealer login credential, bound from the "DealerAuth" config section.
/// Keep real values only in gitignored appsettings.Production.json.
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

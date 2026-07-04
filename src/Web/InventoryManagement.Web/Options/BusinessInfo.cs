namespace InventoryManagement.Web.Options;

/// <summary>
/// Single source of truth for business/legal details shown on the public policy pages
/// (and reused later by the public catalog's contact blocks). Bound from the
/// "BusinessInfo" section of appsettings — edit the values there, not in the pages.
/// </summary>
public class BusinessInfo
{
    public const string SectionName = "BusinessInfo";

    public string LegalName { get; set; } = "AutoNex Thrissur";
    public string BrandName { get; set; } = "AutoNex";
    public string Address { get; set; } = "Thrissur, Kerala, India";
    public string SupportEmail { get; set; } = "support@example.com";
    public string SupportPhone { get; set; } = "+91 00000 00000";
    public string BusinessHours { get; set; } = "Mon–Sat, 10:00 AM – 7:00 PM IST";
    public string GrievanceOfficerName { get; set; } = "Madhav";
    public int RefundWindowDays { get; set; } = 7;
    public string ProcessingTimeText { get; set; } = "2–4 business days";
    public string Jurisdiction { get; set; } = "Thrissur, Kerala";
    public string UdyamRegistrationNumber { get; set; } = "";
    public string WebsiteUrl { get; set; } = "https://neologixchapter.runasp.net";

    /// <summary>Date shown as "last updated" on policy pages. Set in config; defaults are fine.</summary>
    public string LastUpdated { get; set; } = "June 2026";
}

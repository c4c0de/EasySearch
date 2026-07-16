using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Data;
using InventoryManagement.Web.Options;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

/// <summary>
/// One-time, additive seeding of admin-editable content. Runs on every startup but only inserts
/// when a table is EMPTY — existing edits are never overwritten, and a fresh production deploy
/// renders exactly what the hardcoded pages rendered before.
/// </summary>
public static class SeedContent
{
    public static async Task EnsureSeededAsync(AppDbContext db, BusinessInfo biz, DealerAuth auth)
    {
        // ---- Admin credential (independent of the other seeds — existing prod DBs get it added) ----
        if (!await db.SiteSettings.AnyAsync(s => s.Key == AuthKeys.PasswordHash))
        {
            // Seed from config: prefer the configured hash, else hash the configured plaintext once.
            var hash = !string.IsNullOrWhiteSpace(auth.PasswordHash)
                ? auth.PasswordHash
                : PasswordHasher.Hash(auth.Password);
            db.SiteSettings.Add(new SiteSetting { Key = AuthKeys.PasswordHash, Value = hash });
        }
        if (!await db.SiteSettings.AnyAsync(s => s.Key == AuthKeys.Username))
            db.SiteSettings.Add(new SiteSetting { Key = AuthKeys.Username, Value = auth.Username });

        // ---- Settings ----
        if (!await db.SiteSettings.AnyAsync())
        {
            db.SiteSettings.AddRange(
                new SiteSetting { Key = SettingKeys.BrandName, Value = biz.BrandName },
                new SiteSetting { Key = SettingKeys.LegalName, Value = biz.LegalName },
                new SiteSetting { Key = SettingKeys.SupportEmail, Value = biz.SupportEmail },
                new SiteSetting { Key = SettingKeys.SupportPhone, Value = biz.SupportPhone },
                new SiteSetting { Key = SettingKeys.BusinessHours, Value = biz.BusinessHours },
                new SiteSetting { Key = SettingKeys.GrievanceOfficerName, Value = biz.GrievanceOfficerName },
                new SiteSetting { Key = SettingKeys.RefundWindowDays, Value = biz.RefundWindowDays.ToString() },
                new SiteSetting { Key = SettingKeys.ProcessingTimeText, Value = biz.ProcessingTimeText },
                new SiteSetting { Key = SettingKeys.Jurisdiction, Value = biz.Jurisdiction },
                new SiteSetting { Key = SettingKeys.UdyamRegistrationNumber, Value = biz.UdyamRegistrationNumber },
                new SiteSetting { Key = SettingKeys.WebsiteUrl, Value = biz.WebsiteUrl },
                new SiteSetting { Key = SettingKeys.LastUpdated, Value = biz.LastUpdated },
                new SiteSetting { Key = SettingKeys.LogoUrl, Value = "" },
                new SiteSetting { Key = SettingKeys.Theme, Value = Theming.ThemeCatalog.DefaultKey });
        }

        // ---- Branch (from the configured address) ----
        if (!await db.Branches.AnyAsync())
        {
            db.Branches.Add(new Branch
            {
                Id = Guid.NewGuid(),
                DealerId = AppConstants.DefaultDealerId,
                Name = "Main Branch",
                Address = biz.Address,
                Phone = biz.SupportPhone,
                Email = biz.SupportEmail,
                Hours = biz.BusinessHours,
                IsPrimary = true,
                SortOrder = 0
            });
        }

        // ---- Content pages ----
        if (!await db.ContentPages.AnyAsync())
        {
            foreach (var (slug, title, markdown) in DefaultPages)
                db.ContentPages.Add(new ContentPage
                {
                    Id = Guid.NewGuid(),
                    Slug = slug,
                    Title = title,
                    Markdown = markdown,
                    UpdatedAt = DateTime.UtcNow
                });
        }

        await db.SaveChangesAsync();
    }

    public static readonly (string Slug, string Title, string Markdown)[] DefaultPages =
    [
        ("terms", "Terms & Conditions",
"""
These Terms & Conditions ("Terms") govern your use of the website {{WebsiteUrl}} and the services offered by **{{LegalName}}** ("we", "us", "our"). By accessing or using our website, you agree to be bound by these Terms. If you do not agree, please do not use the site.

## 1. About us

{{LegalName}} operates an online catalogue of automobile spare parts based in {{Jurisdiction}}, India. Customers can browse listings and contact us to enquire about, reserve, or purchase parts.

## 2. Eligibility

You must be at least 18 years of age and capable of entering into a legally binding contract to use our services.

## 3. Products and listings

We make reasonable efforts to display product details, availability, condition, and pricing accurately. However, listings may contain errors and availability can change without notice. We reserve the right to correct any errors and to refuse or cancel an order arising from such an error.

## 4. Pricing and payment

Prices are listed in Indian Rupees (₹) and are described on our [Pricing](/pricing) page. Online payments, where offered, are processed through our payment partner (Razorpay). We do not store your full card or banking details.

## 5. Orders, cancellations and refunds

Order confirmation, cancellation, and refund terms are described in our [Refund & Cancellation Policy](/refund). Delivery terms are described in our [Shipping & Delivery Policy](/shipping).

## 6. Acceptable use

You agree not to misuse the website, attempt to gain unauthorised access, or use it for any unlawful purpose. We may suspend access for conduct that we consider harmful to us or other users.

## 7. Intellectual property

All content on this site — including text, images, and branding — is owned by or licensed to {{LegalName}} and may not be reproduced without permission.

## 8. Limitation of liability

To the maximum extent permitted by law, {{LegalName}} shall not be liable for any indirect or consequential loss arising from the use of this website. Nothing in these Terms limits liability that cannot be excluded under applicable law.

## 9. Governing law

These Terms are governed by the laws of India, and any disputes are subject to the exclusive jurisdiction of the courts at {{Jurisdiction}}.

## 10. Changes

We may update these Terms from time to time. Continued use of the website after changes constitutes acceptance of the revised Terms.

## 11. Contact

Questions about these Terms? See our [Contact Us](/contact) page or email [{{SupportEmail}}](mailto:{{SupportEmail}}).
"""),

        ("privacy", "Privacy Policy",
"""
{{LegalName}} ("we", "us", "our") respects your privacy. This policy explains what personal information we collect, why we collect it, and how we use and protect it.

## 1. Information we collect

- **Contact details** you provide when enquiring or ordering — name, phone number, email address, and delivery address.
- **Enquiry / order details** — the products you ask about or purchase.
- **Payment information** — processed by our payment partner; we do not store full card or bank account numbers.
- **Usage data** — basic technical information such as browser type and pages visited, used to keep the site working and secure.

## 2. Why we collect it

We use your information to respond to enquiries, process and deliver orders, provide customer support, comply with legal obligations, and improve our services.

## 3. How we share it

We share information only as needed to run our business: with our payment processor (Razorpay) to complete transactions, with logistics/delivery partners to fulfil orders, and where required by law. We do not sell your personal information.

## 4. Cookies

The site uses essential cookies required for it to function. We do not use them to track you across other websites.

## 5. Data security

We take reasonable technical and organisational measures to protect your information. Online payments are handled over secure, encrypted connections by our payment partner.

## 6. Data retention

We retain personal information only for as long as necessary to fulfil the purposes described here or as required by law.

## 7. Your rights

You may request access to, correction of, or deletion of your personal information by contacting us at [{{SupportEmail}}](mailto:{{SupportEmail}}).

## 8. Grievance Officer

In accordance with applicable Indian law, the Grievance Officer is:

**{{GrievanceOfficerName}}**
{{LegalName}}, {{Address}}
Email: [{{SupportEmail}}](mailto:{{SupportEmail}})
Phone: {{SupportPhone}} ({{BusinessHours}})

## 9. Changes

We may update this policy periodically. The latest version will always be available on this page.
"""),

        ("refund", "Refund & Cancellation Policy",
"""
This policy explains how cancellations, returns, and refunds work for purchases from **{{LegalName}}**.

## 1. Order cancellation

You may request cancellation of an order before it has been dispatched by contacting us at [{{SupportEmail}}](mailto:{{SupportEmail}}) or {{SupportPhone}}. Once an order has been dispatched, it is treated as a return (see below).

## 2. Returns

If a part is defective, damaged in transit, or materially different from its description, you may request a return within **{{RefundWindowDays}} days** of delivery. The item must be unused, in its original condition and packaging, with proof of purchase.

Because automobile parts vary by vehicle and fitment, please confirm compatibility before purchase. Items damaged through incorrect installation or normal wear are not eligible for return.

## 3. Non-returnable items

Electrical parts, custom-ordered items, and items marked "no return" on the listing are not eligible for return unless they arrive defective or damaged.

## 4. Refund process & timelines

Once your return is received and inspected, we will notify you of approval. **Approved refunds are processed within {{RefundWindowDays}} business days** to the original payment method (net of the courier charge — see below). The time for the amount to reflect in your account depends on your bank or card issuer.

Online payment refunds are issued through our payment partner (Razorpay) to the original payment source.

## 5. Courier / delivery charges

The listed price of each item is **inclusive of courier and delivery charges**. These delivery charges are **non-refundable**: for eligible returns, the applicable courier charge is **deducted from your refund** and the remaining balance is refunded to you.

The only exception is where the return is due to our error (wrong, defective, or damaged item) — in that case we bear the courier cost and refund the full amount you paid.

## 6. How to request

To start a cancellation or return, contact us at [{{SupportEmail}}](mailto:{{SupportEmail}}) or {{SupportPhone}} ({{BusinessHours}}) with your order details.
"""),

        ("shipping", "Shipping & Delivery Policy",
"""
This policy describes how orders from **{{LegalName}}** are dispatched and delivered.

## 1. Processing time

Orders are typically processed and dispatched within **{{ProcessingTimeText}}** of order confirmation, subject to stock availability. You will be notified if an item requires additional time.

## 2. Delivery

We deliver through reputable courier and logistics partners. Delivery timelines depend on your location and the courier; metro areas are usually faster than remote locations. Large or heavy automobile parts may require specialised transport, which can affect timelines.

## 3. Delivery areas

We primarily serve customers within India. For locations outside our standard service area, please contact us at [{{SupportEmail}}](mailto:{{SupportEmail}}) before ordering to confirm availability and charges.

## 4. Shipping charges

Shipping charges, where applicable, are calculated based on the size, weight, and destination of the order and are shown or communicated before payment.

## 5. Tracking

Where the courier provides tracking, we will share the tracking details with you after dispatch.

## 6. Delays

We are not responsible for delays caused by couriers, weather, or other circumstances beyond our control, but we will assist you in following up on any delayed shipment.

## 7. Local pickup

Local pickup from {{Jurisdiction}} may be available by arrangement. Contact us at {{SupportPhone}} ({{BusinessHours}}) to coordinate.
"""),

        ("pricing", "Pricing",
"""
This page explains how pricing works at **{{LegalName}}**.

## 1. Product pricing

Each part is individually priced and the price is shown on its listing. All prices are in Indian Rupees (₹). Because automobile spare parts vary by brand, condition (new / used / refurbished), and vehicle fitment, prices differ from item to item.

## 2. Taxes

Applicable taxes, where charged, are shown or communicated before you complete payment. If a price is inclusive or exclusive of taxes, this will be indicated at the time of purchase.

## 3. Quotations

For items without a displayed price, or for bulk enquiries, please contact us for a quotation at [{{SupportEmail}}](mailto:{{SupportEmail}}) or {{SupportPhone}}.

## 4. Price changes

Prices and availability may change without prior notice. The price applicable to your order is the one confirmed at the time of purchase.

## 5. Payments

Online payments, where offered, are processed securely through our payment partner (Razorpay). There are no hidden charges; any delivery or handling fees will be shown before payment. See our [Refund & Cancellation Policy](/refund) for cancellations and refunds.
"""),

        ("contact-intro", "Contact Us",
"""
We're happy to help with product enquiries, orders, and support. Reach any of our branches below.
"""),
    ];
}

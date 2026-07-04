# Plan: Swappable UI templates, external image hosting (not Google Photos), car-make suggestions

## Context
Three asks for the white-label product:
1. **"Entirely different UI per client, not just colors"** → swappable public **templates** (layout + page structure + card design), picked in Site Settings. Approved: start with **Classic** (current) + **Showroom** (hero search, horizontal filter chips, image-first large cards). Combined with the 5 color themes = 10 distinct looks.
2. **Free-plan image limits → Google Photos?** User asked if per-part-type Google Photos "libraries" are feasible. **Verdict: technically possible, practically a trap** (see below) → instead: an **Image-URL option with live preview** (works with any hosted image) + a **Cloudinary storage backend** (free 25 GB CDN; per-part-type folders give the "library" organization the user wants).
3. **Car-name filter suggestions** — the public and admin filter sidebars get autocomplete from existing vehicle makes (the add-part form already has this via `GetDistinctMakesAsync` + `<datalist>`).

### Why not Google Photos albums (answer for the user, keep in summary)
Post-March-2025 API rules: an app *can* upload and read back only **its own** uploads — but image `baseUrl`s **expire after ~60 minutes** and byte-access is quota-limited, so every public page view would need fresh Google API calls (dealer OAuth + token refresh on the 256 MB host, Google app verification for sensitive scopes) or a proxy that pipes bytes through MonsterASP — consuming the very bandwidth we're trying to save, with Google quotas on top, and Google signalling further lockdowns through June 2026. Cloudinary is built for exactly this job: permanent CDN URLs, free tier, folder-per-part-type organization, browsable media library UI.

**Prod-safe bonus: no DB migration needed for any of this** — template choice is a new `SiteSettings` KV row (lazy), Cloudinary is config-only, autocomplete is UI-only.

---

## A. Swappable public templates
- `SiteSettings` key `Template` (`classic` | `showroom`), default `classic` ⇒ zero change until switched.
- **New** `Web/Theming/TemplateCatalog.cs` — template metadata (key, name, description) for the picker.
- **Refactor** `PublicCatalog.razor` into a thin dispatcher: loads `SiteConfig`, renders `<ClassicCatalog/>` or `<ShowroomCatalog/>`.
  - **New** `Components/Public/CatalogBase.cs` — shared `ComponentBase` subclass holding ALL existing filter/debounce/paging state + `IDisposable` (moved verbatim from today's PublicCatalog `@code`).
  - **New** `Components/Public/ClassicCatalog.razor` (`@inherits CatalogBase`) — current markup (sidebar + grid + `PublicListingCard`), moved as-is.
  - **New** `Components/Public/ShowroomCatalog.razor` (`@inherits CatalogBase`) — different structure: full-width hero (brand tagline + big search box), **horizontal filter chip bar** (make datalist input, year, part-type dropdown, price, availability — no sidebar), large image-first cards (**new** `ShowroomListingCard.razor`: big 4:3 image, overlay price badge, contact buttons on hover/footer), different pagination styling.
- `PublicLayout.razor` — template-aware header: `showroom` gets a transparent/overlay header class; theme `<style>` emission unchanged (themes apply to both templates since both use the same CSS variables).
- `Settings.razor` — "Layout Template" picker section (cards with mini wireframe sketches drawn in CSS), above the theme picker.
- `app.css` — showroom styles (`.hero`, `.filter-chips`, `.showroom-card`, overlay header) written against the existing variables so all 5 themes color both templates.

## B. Images: URL option + Cloudinary backend
- **`ImageUpload.razor`** — add a mode toggle: **Upload** (existing) | **Image URL**. URL mode: text input + **live preview** `<img>` (with `onerror` → "couldn't load this image" warning), basic validation (http/https), a hint that Google Photos share links are unstable and a proper host (Cloudinary/Imgur) is recommended. Emits via the same `OnUploaded` callback ⇒ automatically available in Add Part, card edit, and the logo uploader with no caller changes.
- **New** `Infrastructure/Storage/CloudinaryStorageService.cs : IStorageService`:
  - `UploadAsync` → multipart POST to `https://api.cloudinary.com/v1_1/{CloudName}/image/upload` with an **unsigned upload preset** (no SDK; `HttpClient`); optional `folder` param = sanitized part-type/purpose passed through the existing filename arg convention (`{folder}/{file}` prefix) — gives per-part-type "libraries" in Cloudinary's media browser.
  - `DeleteAsync` → signed `destroy` call when `ApiKey`+`ApiSecret` configured (SHA-1 signature), else warn-log no-op.
  - Config: `Cloudinary:CloudName`, `Cloudinary:UploadPreset`, optional `Cloudinary:ApiKey/ApiSecret` in `appsettings.Production.json` (gitignored, same pattern as AWS/DealerAuth). Placeholder block + comment in `appsettings.json`.
  - `DependencyInjection.cs` storage selection: **Cloudinary if configured → else AWS if configured → else local** (existing behavior untouched when keys absent). Register `HttpClient` for the service.
- Cards/pages need no changes — they already render whatever URL the listing carries.

## C. Car-make autocomplete on filters
- `ClassicCatalog` (and `ShowroomCatalog`) + admin `Inventory/Index.razor`: change the Car Make filter input to `list="make-suggestions"` + `<datalist>` populated once in `OnInitializedAsync` via existing `IInventoryRepository.GetDistinctMakesAsync` (add the load to `CatalogBase`). Typing still free-text (contains-filter unchanged); suggestions show existing cars.

## Files
- New: `Web/Theming/TemplateCatalog.cs`; `Components/Public/{CatalogBase.cs,ClassicCatalog.razor,ShowroomCatalog.razor,ShowroomListingCard.razor}`; `Infrastructure/Storage/CloudinaryStorageService.cs`.
- Edit: `PublicCatalog.razor` (dispatcher), `PublicLayout.razor` (header variant), `Settings.razor` (template picker), `ImageUpload.razor` (URL mode), `Inventory/Index.razor` (datalist), `SiteConfigService.cs` (`Template` key), `DependencyInjection.cs`, `appsettings.json` (+Production placeholder), `app.css`.
- No migration; no changes to existing tables/routes.

## Reuse
- `SiteConfig`/`SettingKeys` + Settings save/invalidate flow; `ThemeCatalog` picker pattern for the template picker; `GetDistinctMakesAsync` + datalist pattern from `CreateListing.razor`; `IStorageService` abstraction (3rd implementation); existing card markup for Classic.

## Verification (build + preview_*)
1. Build clean; default template = `classic` → public site byte-identical behavior (filters, cards, contact buttons, pagination).
2. Settings → switch to **Showroom** → `/` renders hero + chip bar + large cards, no sidebar; filters/search/paging still work (same `CatalogBase` logic); WhatsApp/Instagram buttons intact; switch themes (emerald/royal) → Showroom recolors; admin untouched. Switch back to Classic → original layout.
3. ImageUpload URL mode: paste a valid image URL → live preview shows, save part → card displays it; paste a broken URL → inline warning, save blocked until cleared. Upload mode still works (local storage).
4. Cloudinary: without config → DI logs/uses LocalStorageService (verify via startup log or behavior); with dummy config → upload path attempts Cloudinary (verify request construction fails gracefully with clear error). Real end-to-end needs the user's free account keys — document setup steps in the summary.
5. Car Make filter on public + admin shows datalist suggestions of existing makes (verify `<datalist>` options in DOM); free-text filtering unchanged.
6. Regression: login, part CRUD, contacts, branches, pages, policy pages, `/health`; `preview_logs` clean. Publish `-r win-x64` ready.

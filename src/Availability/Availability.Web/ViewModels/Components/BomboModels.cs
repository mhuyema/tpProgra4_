namespace Availability.Web.Models.Components;

public enum ButtonVariant { Primary, Secondary }

public record ButtonModel(
    string Text,
    string? Href = null,
    ButtonVariant Variant = ButtonVariant.Primary,
    string Type = "button",
    bool Lock = false,
    string LockText = "Procesando…");

public record ChipModel(string Text, bool Active = false, string? Href = null);

public record BadgeModel(string Text, bool IsError = false);

public record SearchInputModel(string Name = "q", string Placeholder = "Buscar eventos");

public record EventCardModel(
    string Title,
    string Meta,
    string ImageUrl,
    string Href,
    string? BadgeText = null,
    bool BadgeIsError = false);

public record NavLinkModel(string Text, string Controller, string Action);

public record NavbarModel(
    string Brand,
    IReadOnlyList<NavLinkModel> Links,
    bool ShowSearch = false,
    IReadOnlyList<string>? Regions = null,
    ButtonModel? Login = null);

public record HeroModel(
    string Title,
    string Meta,
    string Description,
    string ImageUrl,
    string CtaText,
    string CtaHref,
    string? LiveText = null);

public record VenueCardModel(string Name, string Location, string ImageUrl, string Href);

public record FooterLink(string Text, string Href);

public record FooterModel(string Brand, IReadOnlyList<FooterLink> Links);

namespace Spillgebees.Blazor.Map.Models.Popups;

/// <summary>
/// Options for configuring a popup on a feature.
/// </summary>
/// <param name="Content">The HTML content of the popup.</param>
/// <param name="Trigger">How the popup is triggered. Default is <see cref="PopupTrigger.Click"/>.</param>
/// <param name="Anchor">The anchor position of the popup relative to the feature. Default is <see cref="PopupAnchor.Auto"/>.</param>
/// <param name="Offset">Optional pixel offset from the default popup position.</param>
/// <param name="CloseButton">Whether to show a close button on the popup. Default is <see langword="true"/>.</param>
/// <param name="MaxWidth">CSS max-width for the popup (e.g., <c>"240px"</c>). Default is <see langword="null"/>.</param>
/// <param name="ClassName">A custom CSS class to apply to the popup container. Default is <see langword="null"/>.</param>
public record PopupOptions(
    string Content,
    PopupTrigger Trigger = PopupTrigger.Click,
    PopupAnchor Anchor = PopupAnchor.Auto,
    Point? Offset = null,
    bool CloseButton = true,
    string? MaxWidth = null,
    string? ClassName = null
);

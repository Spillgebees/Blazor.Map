using System.Collections.Immutable;

namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// Options for fitting the map to provided bounds.
/// </summary>
/// <param name="LayerIds">The layers to calculate bounds for and fit the map to.</param>
/// <param name="TopLeftPadding">Padding to add from the top left.</param>
/// <param name="BottomRightPadding">Padding to add from the bottom right.</param>
/// <param name="Padding">Equivalent to setting the same padding for <see cref="TopLeftPadding" /> and <see cref="BottomRightPadding" />.</param>
public record FitBoundsOptions(
    ImmutableList<string> LayerIds,
    Point? TopLeftPadding = null,
    Point? BottomRightPadding = null,
    Point? Padding = null);

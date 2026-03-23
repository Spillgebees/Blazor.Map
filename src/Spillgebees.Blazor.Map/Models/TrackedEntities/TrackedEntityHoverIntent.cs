namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Hover interaction intent for a tracked entity.
/// </summary>
/// <param name="Scale">Optional scale multiplier to apply while hovered.</param>
/// <param name="RaiseToTop">Whether the entity should be visually prioritized while hovered.</param>
public sealed record TrackedEntityHoverIntent(double? Scale = null, bool RaiseToTop = false);

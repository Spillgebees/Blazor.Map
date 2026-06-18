namespace Spillgebees.Blazor.Map;

/// <summary>
/// When an active follow clears on its own. This covers the pan gesture and the followed entity going missing.
/// Programmatic camera moves, including the follow's own, never clear it.
/// </summary>
/// <param name="ClearOnUserPan">Clear when the user drags the map. Default true so the map never traps the user.</param>
/// <param name="ClearWhenFeatureMissing">
/// Clear when the followed entity is no longer present. Default false, allowing temporary gaps.
/// </param>
public sealed record MapFollowInteractionOptions(bool ClearOnUserPan = true, bool ClearWhenFeatureMissing = false);

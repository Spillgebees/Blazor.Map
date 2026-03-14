namespace Spillgebees.Blazor.Map.Models.Events;

/// <summary>
/// Event arguments for a map click event.
/// </summary>
/// <param name="Position">The geographical coordinate where the click occurred.</param>
public record MapClickEventArgs(Coordinate Position);

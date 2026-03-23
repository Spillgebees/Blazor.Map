namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record LayerEventDescriptor(
    string LayerId,
    object DotNetRef,
    bool OnClick,
    bool OnMouseEnter,
    bool OnMouseLeave
);

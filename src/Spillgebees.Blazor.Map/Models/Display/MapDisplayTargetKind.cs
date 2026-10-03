namespace Spillgebees.Blazor.Map;

/// <summary>How a <see cref="MapDisplayTarget"/> resolves to map layers.</summary>
internal enum MapDisplayTargetKind
{
    /// <summary>Runtime layers by layer id or owning component id.</summary>
    Layers,

    /// <summary>Style layers by their id in the style JSON.</summary>
    StyleLayers,

    /// <summary>Style layers by metadata tag.</summary>
    StyleTags,

    /// <summary>Every layer of a style; hide-only.</summary>
    Style,
}

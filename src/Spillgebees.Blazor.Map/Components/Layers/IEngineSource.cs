namespace Spillgebees.Blazor.Map;

/// <summary>
/// Cascaded by engine source components so nested layers can resolve their source id
/// and force source creation ahead of their own layer ops.
/// </summary>
internal interface IEngineSource
{
    string Id { get; }

    void EnsureInitialized();
}

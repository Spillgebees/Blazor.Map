using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// The fullscreen surface of <see cref="SgbMap" />: an imperative API for toggling fullscreen
/// without placing a control, and the change notification that keeps host UI in sync. Both the
/// built-in <see cref="FullscreenMapControl" /> and these methods drive the one shared fullscreen
/// primitive in the engine, so state stays consistent across the control, this API, and the user
/// pressing Esc.
/// </summary>
public partial class SgbMap
{
    /// <summary>
    /// Whether the map is currently presented fullscreen. Updated from every source — the built-in
    /// control, the imperative API, and the browser (e.g. the user pressing Esc). Observe-only: the
    /// browser owns fullscreen state, so this is not a two-way-bindable parameter (there is no
    /// <c>@bind-IsFullscreen</c>); use <see cref="OnFullscreenChanged"/> to react to changes.
    /// </summary>
    public bool IsFullscreen { get; private set; }

    /// <summary>
    /// Raised whenever the fullscreen state changes, regardless of what triggered it. This is the
    /// observe-only counterpart to <c>@bind</c> — fullscreen state cannot be pushed via a bound
    /// parameter because the browser grants and revokes it; drive changes with
    /// <see cref="ToggleFullscreenAsync"/> / <see cref="EnterFullscreenAsync"/> instead.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnFullscreenChanged { get; set; }

    /// <summary>
    /// Presents the map fullscreen. Browsers only grant fullscreen in response to a user gesture,
    /// so call this from a user-initiated event (e.g. a button click); otherwise the request may be
    /// denied and the call becomes a no-op. Observe the result via <see cref="OnFullscreenChanged" />.
    /// </summary>
    public Task EnterFullscreenAsync() => Channel.QueueAndFlushAsync(new FullscreenSetOp(true));

    /// <summary>Exits fullscreen.</summary>
    public Task ExitFullscreenAsync() => Channel.QueueAndFlushAsync(new FullscreenSetOp(false));

    /// <summary>
    /// Toggles the fullscreen state. Entering fullscreen requires a user gesture (see
    /// <see cref="EnterFullscreenAsync" />); observe the result via <see cref="OnFullscreenChanged" />.
    /// </summary>
    public Task ToggleFullscreenAsync() => Channel.QueueAndFlushAsync(new FullscreenSetOp());

    // Routed from the map-event channel ("fullscreenchanged"): the state changed from any source —
    // the control, the imperative API, or the browser (e.g. the user pressing Esc).
    private Task HandleFullscreenChangedAsync(JsonElement payload) =>
        SetFullscreenStateAsync(payload.GetProperty("isFullscreen").GetBoolean());

    private async Task SetFullscreenStateAsync(bool isFullscreen)
    {
        if (IsFullscreen == isFullscreen)
        {
            return;
        }

        IsFullscreen = isFullscreen;
        if (OnFullscreenChanged.HasDelegate)
        {
            await OnFullscreenChanged.InvokeAsync(isFullscreen);
        }
    }
}

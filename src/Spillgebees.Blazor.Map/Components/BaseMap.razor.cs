using System.Diagnostics.Tracing;
using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Interop;

namespace Spillgebees.Blazor.Map.Components;

public abstract partial class BaseMap : ComponentBase, IAsyncDisposable
{
    [Inject]
    protected IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private ILoggerFactory _loggerFactory { get; set; } = null!;
    protected Lazy<ILogger> Logger => new(() => _loggerFactory.CreateLogger(GetType()));

    [Parameter, EditorRequired]
    public required Coordinate Center { get; set; }

    [Parameter]
    public int Zoom { get; set; } = 9;

    [Parameter]
    public RenderFragment? MapContent { get; set; }

    [Parameter]
    public string MapContainerHtmlId { get; set; } = $"map-container-{Guid.NewGuid()}";

    [Parameter]
    public string MapContainerClass { get; set; } = string.Empty;

    protected string InternalMapContainerClass => new CssBuilder()
        .AddClass("sgb-map-container")
        .AddClass(MapContainerClass)
        .Build();

    protected ElementReference MapReference;
    protected DotNetObjectReference<BaseMap>? DotNetObjectReference;
    protected bool IsInitialized;
    protected bool IsDisposing;

    private readonly TaskCompletionSource _initializationCompletionSource = new();

    public virtual async ValueTask DisposeAsync()
    {
        if (IsDisposing)
        {
            return;
        }
        IsDisposing = true;

        try
        {
            // ensure initialization has been completed to avoid DotNetObjectReference disposed exceptions
            await _initializationCompletionSource.Task;
            await MapJs.DisposeMapAsync(JsRuntime, Logger.Value, MapReference);
        }
        catch (Exception exception) when (exception is JSDisconnectedException or OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Logger.Value.LogError(exception, "Failed to dispose editor");
        }
        finally
        {
            DotNetObjectReference?.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    [JSInvokable]
    public async Task OnMapInitializedAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        _initializationCompletionSource.TrySetResult();
        IsInitialized = true;

        // ensure map is initialized correctly
        await Task.Delay(50);
        await InvalidateMapSizeAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsInitialized is false)
        {
            return;
        }

        await Task.CompletedTask;
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
        => firstRender
            ? InitializeEditorAsync()
            : Task.CompletedTask;

    protected virtual async Task InitializeEditorAsync()
    {
        DotNetObjectReference = Microsoft.JSInterop.DotNetObjectReference.Create(this);

        await MapJs.CreateMapAsync(
            JsRuntime,
            Logger.Value,
            DotNetObjectReference,
            nameof(OnMapInitializedAsync),
            MapReference,
            Center,
            Zoom);
    }

    private ValueTask InvalidateMapSizeAsync()
        => MapJs.InvalidateSizeAsync(JsRuntime, Logger.Value, MapReference);
}

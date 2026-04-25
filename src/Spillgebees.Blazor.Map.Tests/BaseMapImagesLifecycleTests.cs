using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests;

public class BaseMapImagesLifecycleTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string SetImagesIdentifier = "Spillgebees.Map.mapFunctions.setImages";
    private const string GetProtocolVersionIdentifier = "Spillgebees.Map.getProtocolVersion";

    private const int TestTimeoutMs = 5000;

    public BaseMapImagesLifecycleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.Setup<int>(GetProtocolVersionIdentifier).SetResult(13);
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(SetImagesIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_images_when_map_becomes_ready(CancellationToken cancellationToken)
    {
        // arrange
        var images = new List<MapImageDefinition>
        {
            new("train-red", "data:image/svg+xml,%3Csvg%3E%3C/svg%3E", 28, 28, sdf: true),
        };
        var cut = Render<SgbMap>(parameters => parameters.Add(p => p.Images, images));

        // act
        await cut.Instance.OnMapInitializedAsync();

        // assert
        JSInterop.VerifyInvoke(SetImagesIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_sync_images_when_images_parameter_changes_after_initialization(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var initialImages = new List<MapImageDefinition>
        {
            new("train-red", "data:image/svg+xml,%3Csvg%3E%3C/svg%3E", 28, 28),
        };
        var cut = Render<SgbMap>(parameters => parameters.Add(p => p.Images, initialImages));
        await cut.Instance.OnMapInitializedAsync();
        var initialSetImagesCallCount = JSInterop.Invocations[SetImagesIdentifier].Count;

        // act
        cut.Render(parameters =>
            parameters.Add(
                p => p.Images,
                new List<MapImageDefinition>
                {
                    new("train-blue", "data:image/svg+xml,%3Csvg%3E%3Crect/%3E%3C/svg%3E", 32, 32, pixelRatio: 2),
                }
            )
        );

        // assert
        JSInterop.Invocations[SetImagesIdentifier].Count.Should().Be(initialSetImagesCallCount + 1);

        var invocation = JSInterop.Invocations[SetImagesIdentifier][^1];
        var imagesPayload = invocation.Arguments[1].Should().BeAssignableTo<Array>().Subject;
        imagesPayload.Length.Should().Be(1);

        var firstImagePayload = imagesPayload.GetValue(0);
        firstImagePayload.Should().NotBeNull();
        GetRequiredPropertyValue(firstImagePayload!, "Name").Should().Be("train-blue");
        GetRequiredPropertyValue(firstImagePayload!, "PixelRatio").Should().Be(2d);
        GetRequiredPropertyValue(firstImagePayload!, "Sdf").Should().BeOfType<bool>();
        ((bool)GetRequiredPropertyValue(firstImagePayload!, "Sdf")).Should().BeFalse();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_force_sync_images_when_style_reloads(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>(parameters =>
            parameters.Add(
                p => p.Images,
                new List<MapImageDefinition>
                {
                    new("train-red", "data:image/svg+xml,%3Csvg%3E%3C/svg%3E", 28, 28, sdf: true),
                }
            )
        );
        await cut.Instance.OnMapInitializedAsync();
        var initialSetImagesCallCount = JSInterop.Invocations[SetImagesIdentifier].Count;

        // act
        await cut.Instance.OnMapStyleReloadedAsync();

        // assert
        JSInterop.Invocations[SetImagesIdentifier].Count.Should().Be(initialSetImagesCallCount);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_mark_add_image_async_as_obsolete(CancellationToken cancellationToken)
    {
        // arrange & act
        var obsoleteAttribute = typeof(SgbMap)
            .GetMethod(nameof(SgbMap.AddImageAsync))
            ?.GetCustomAttributes(false)
            .OfType<ObsoleteAttribute>()
            .SingleOrDefault();

        // assert
        obsoleteAttribute.Should().NotBeNull();
        obsoleteAttribute!.Message.Should().Contain("Images");
    }

    private static object GetRequiredPropertyValue(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        property.Should().NotBeNull($"property {propertyName} should exist on {source.GetType().Name}");

        var value = property!.GetValue(source);
        value.Should().NotBeNull($"property {propertyName} should have a value");
        return value!;
    }
}

`Spillgebees.Blazor.Map` is a Blazor map component powered by [Leaflet](https://github.com/Leaflet/Leaflet).

### Registering the component

This component comes with a [JS initializer](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/startup?view=aspnetcore-10.0#javascript-initializers), as such it is bootstrapped when `Blazor` launches.

The only thing you need to do is to add the Leaflet CSS file for styling.

Include it in the `head` tag:

```html
<link href="_content/Spillgebees.Blazor.Map/Spillgebees.Blazor.Map.lib.module.css"
      rel="stylesheet" />
```

### Usage

You can take a look at the demo pages for a few general usage examples: [net8.0](https://spillgebees.github.io/Blazor.Map/main/net8.0/), [net9.0](https://spillgebees.github.io/Blazor.Map/main/net9.0/), [net10.0](https://spillgebees.github.io/Blazor.Map/main/net10.0/)


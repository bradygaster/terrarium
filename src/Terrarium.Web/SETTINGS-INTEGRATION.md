# Settings Integration Guide

## Overview
The Settings page (`/settings`) persists user preferences to localStorage. This guide shows how to read those settings in other components.

## Settings Schema

```json
{
  "EcosystemMode": "local" | "networked",
  "ServerUrl": "https://terrarium-server.azurewebsites.net",
  "ZoomLevel": 1.0,
  "ShowMinimap": true,
  "ShowFpsCounter": false,
  "Theme": "classic" | "dark"
}
```

## Reading Settings in Components

### Step 1: Inject IJSRuntime
```csharp
@inject IJSRuntime JS
```

### Step 2: Load settings on component init
```csharp
private TerrariumSettings? _settings;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await LoadSettings();
    }
}

private async Task LoadSettings()
{
    try
    {
        var json = await JS.InvokeAsync<string?>("localStorage.getItem", "terrarium-settings");
        if (!string.IsNullOrEmpty(json))
        {
            _settings = System.Text.Json.JsonSerializer.Deserialize<TerrariumSettings>(json);
        }
        else
        {
            _settings = new TerrariumSettings(); // defaults
        }
    }
    catch
    {
        _settings = new TerrariumSettings(); // fallback
    }
}

private class TerrariumSettings
{
    public string EcosystemMode { get; set; } = "local";
    public string ServerUrl { get; set; } = "https://terrarium-server.azurewebsites.net";
    public double ZoomLevel { get; set; } = 1.0;
    public bool ShowMinimap { get; set; } = true;
    public bool ShowFpsCounter { get; set; } = false;
    public string Theme { get; set; } = "classic";
}
```

## Integration Points

### 1. GameView Component — Zoom Level

**File:** `Components/GameView.razor`

```csharp
// After loading settings:
if (_settings?.ZoomLevel is double zoom)
{
    await JS.InvokeVoidAsync("TerrariumRenderer.setZoom", zoom);
}

// Or pass to renderer via JS interop:
await JS.InvokeVoidAsync("TerrariumRenderer.init", new
{
    canvasId = "terrarium-canvas",
    zoom = _settings?.ZoomLevel ?? 1.0
});
```

### 2. GameView Component — Show Minimap

**File:** `Components/GameView.razor`

```csharp
// Conditionally render minimap element:
@if (_settings?.ShowMinimap == true)
{
    <div class="minimap-overlay">
        <!-- Minimap canvas or component -->
    </div>
}
```

### 3. GameView Component — Show FPS Counter

**File:** `Components/GameView.razor`

```csharp
// Conditionally render FPS display:
@if (_settings?.ShowFpsCounter == true)
{
    <div class="fps-counter">
        FPS: @_currentFps
    </div>
}

// In your render loop (JS interop):
if (_settings?.ShowFpsCounter == true)
{
    await JS.InvokeVoidAsync("TerrariumRenderer.enableFpsCounter", true);
}
```

### 4. TerrariumHubClient — Server URL

**File:** `Services/TerrariumHubClient.cs`

Modify the constructor or StartAsync method:

```csharp
public async Task StartAsync()
{
    var settingsJson = await _js.InvokeAsync<string?>("localStorage.getItem", "terrarium-settings");
    var settings = string.IsNullOrEmpty(settingsJson)
        ? new { ServerUrl = "https://terrarium-server.azurewebsites.net" }
        : JsonSerializer.Deserialize<dynamic>(settingsJson);
    
    _connection = new HubConnectionBuilder()
        .WithUrl(settings.ServerUrl)
        .WithAutomaticReconnect()
        .Build();
    
    await _connection.StartAsync();
}
```

### 5. Home Component — Ecosystem Mode

**File:** `Components/Pages/Home.razor`

```csharp
protected override async Task OnInitializedAsync()
{
    await LoadSettings();
    
    if (_settings?.EcosystemMode == "networked")
    {
        await HubClient.StartAsync();
    }
    else
    {
        // Local-only mode — no SignalR connection
    }
}
```

### 6. App.razor — Theme Switching

**File:** `Components/App.razor`

Add a script to read theme on page load:

```html
<script>
    // Apply theme from localStorage on load
    (function() {
        const settings = localStorage.getItem('terrarium-settings');
        if (settings) {
            const parsed = JSON.parse(settings);
            if (parsed.Theme === 'dark') {
                document.body.setAttribute('data-theme', 'dark');
            }
        }
    })();
</script>
```

Then in `glass-theme.css`, add dark mode overrides:

```css
[data-theme="dark"] {
    --glass-gradient-panel-top: #404060;
    --glass-gradient-panel-bottom: #000020;
    --glass-color-foreground: #c0c0c0;
    /* ... more dark mode token overrides */
}
```

## Listening for Settings Changes

If you want a component to react to settings changes in real-time (without page reload):

```csharp
// In the component that needs to react:
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // Set up a storage event listener
        await JS.InvokeVoidAsync("eval", @"
            window.addEventListener('storage', (e) => {
                if (e.key === 'terrarium-settings') {
                    DotNet.invokeMethodAsync('Terrarium.Web', 'OnSettingsChanged', e.newValue);
                }
            });
        ");
    }
}

[JSInvokable]
public static async Task OnSettingsChanged(string newSettingsJson)
{
    // Reload and apply new settings
    var settings = JsonSerializer.Deserialize<TerrariumSettings>(newSettingsJson);
    // Apply settings...
}
```

Note: The `storage` event only fires for changes from *other* tabs/windows. For same-page changes, you'd need to emit a custom event from Settings.razor after save.

## Testing Settings

1. Navigate to `/settings`
2. Change ecosystem mode to "Networked"
3. Change zoom to 1.5x
4. Save settings
5. Navigate to `/` (Home)
6. Open browser DevTools > Application > Local Storage
7. Verify `terrarium-settings` key contains JSON

## Defaults

If no settings are found in localStorage, use these defaults:
- Ecosystem Mode: `"local"`
- Server URL: `"https://terrarium-server.azurewebsites.net"`
- Zoom Level: `1.0`
- Show Minimap: `true`
- Show FPS Counter: `false`
- Theme: `"classic"`

These match the defaults in `Settings.razor` and ensure consistent behavior.

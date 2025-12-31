# MediaServerNotification

## Build note: `BrotliCompress` / `DOTNET_HOST_PATH is not set`

This app uses `Microsoft.NET.Sdk.Razor` (Blazor Static Web Assets). During builds, MSBuild may run the `BrotliCompress` task, which requires the environment variable `DOTNET_HOST_PATH` to point at your `dotnet` executable.

### Fix (PowerShell)

- **Temporary (current terminal session only):**

```powershell
$env:DOTNET_HOST_PATH = (Get-Command dotnet).Source
```

- **Permanent (User environment variable):**

```powershell
[Environment]::SetEnvironmentVariable('DOTNET_HOST_PATH', (Get-Command dotnet).Source, 'User')
```

After setting it permanently, restart Visual Studio / Rider and any terminals so the new environment variable is picked up.
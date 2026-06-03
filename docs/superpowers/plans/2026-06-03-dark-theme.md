# Dark Theme Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a toggleable dark/light theme to the Blazor WASM Todo App, persisted in `localStorage` and applied flash-free on page load.

**Architecture:** Upgrade Bootstrap from 5.1.0 to 5.3.x to gain the native `data-bs-theme` attribute, which themes all Bootstrap components automatically. A new `ThemeService` (scoped Blazor DI) manages reading/writing the preference via JS interop. A small inline `<script>` in `App.razor`'s `<head>` applies the theme before Blazor hydrates to prevent a flash of the wrong theme.

**Tech Stack:** Blazor WASM (.NET 9), Bootstrap 5.3.3 (static assets), `IJSRuntime` interop, `localStorage`

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Replace | `Todo.Web/Client/wwwroot/bootstrap/dist/` | Bootstrap 5.3.3 CSS/JS dist files |
| Modify | `Todo.Web/Server/App.razor` | Add inline JS + `data-bs-theme` bootstrap link |
| Create | `Todo.Web/Client/Services/ThemeService.cs` | Reads/writes theme via JS interop |
| Modify | `Todo.Web/Client/Program.cs` | Register `ThemeService` |
| Modify | `Todo.Web/Client/TodoApp.razor` | Theme toggle button (sun/moon) |
| Modify | `Todo.Web/Client/_Imports.razor` | Add `using` for `Services` namespace |
| Modify | `Todo.Web/Client/wwwroot/css/app.css` | Replace hardcoded `white` focus shadow with CSS variable |

---

## Task 1: Upgrade Bootstrap to 5.3.3

**Files:**
- Replace: `Todo.Web/Client/wwwroot/bootstrap/dist/`

- [ ] **Step 1: Download Bootstrap 5.3.3 dist zip**

```powershell
$dest = "D:\TRAININGS\AI Copilot\Projects\TodoApp\Todo.Web\Client\wwwroot\bootstrap"
$zip  = "$env:TEMP\bootstrap-5.3.3-dist.zip"
Invoke-WebRequest -Uri "https://github.com/twbs/bootstrap/releases/download/v5.3.3/bootstrap-5.3.3-dist.zip" -OutFile $zip
Expand-Archive -Path $zip -DestinationPath "$env:TEMP\bootstrap-5.3.3" -Force
```

- [ ] **Step 2: Replace dist folder contents**

```powershell
Remove-Item "$dest\dist" -Recurse -Force
Copy-Item "$env:TEMP\bootstrap-5.3.3\bootstrap-5.3.3-dist" -Destination "$dest\dist" -Recurse
Remove-Item "$env:TEMP\bootstrap-5.3.3", $zip -Recurse -Force
```

- [ ] **Step 3: Verify key files exist**

```powershell
Test-Path "D:\TRAININGS\AI Copilot\Projects\TodoApp\Todo.Web\Client\wwwroot\bootstrap\dist\css\bootstrap.min.css"
Test-Path "D:\TRAININGS\AI Copilot\Projects\TodoApp\Todo.Web\Client\wwwroot\bootstrap\dist\js\bootstrap.bundle.min.js"
```

Expected: both return `True`

- [ ] **Step 4: Confirm version**

```powershell
Select-String -Path "D:\TRAININGS\AI Copilot\Projects\TodoApp\Todo.Web\Client\wwwroot\bootstrap\dist\css\bootstrap.css" -Pattern "Bootstrap v\d" | Select-Object -First 1
```

Expected output contains: `Bootstrap v5.3.3`

- [ ] **Step 5: Commit**

```bash
git add Todo.Web/Client/wwwroot/bootstrap/dist/
git commit -m "chore: upgrade Bootstrap 5.1.0 -> 5.3.3

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

## Task 2: Add Flash-Prevention JS and `data-bs-theme` to App.razor

**Files:**
- Modify: `Todo.Web/Server/App.razor`

- [ ] **Step 1: Add inline theme script to `<head>` (before the Bootstrap CSS link)**

In `Todo.Web/Server/App.razor`, replace:

```razor
<head>
    <meta charset="utf-8" />
    <title>Todo App</title>
    <base href="/" />
    <link rel="stylesheet" href="bootstrap/dist/css/bootstrap.min.css" />
    <link href="css/app.css" rel="stylesheet" />
</head>
```

With:

```razor
<head>
    <meta charset="utf-8" />
    <title>Todo App</title>
    <base href="/" />
    <script>
        window.todoTheme = {
            getTheme: function () { return localStorage.getItem('theme') || 'light'; },
            setTheme: function (theme) {
                localStorage.setItem('theme', theme);
                document.documentElement.setAttribute('data-bs-theme', theme);
            }
        };
        document.documentElement.setAttribute('data-bs-theme', window.todoTheme.getTheme());
    </script>
    <link rel="stylesheet" href="bootstrap/dist/css/bootstrap.min.css" />
    <link href="css/app.css" rel="stylesheet" />
</head>
```

- [ ] **Step 2: Commit**

```bash
git add Todo.Web/Server/App.razor
git commit -m "feat: add flash-prevention theme script to App.razor

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

## Task 3: Create ThemeService

**Files:**
- Create: `Todo.Web/Client/Services/ThemeService.cs`
- Modify: `Todo.Web/Client/Program.cs`
- Modify: `Todo.Web/Client/_Imports.razor`

- [ ] **Step 1: Create the Services directory and ThemeService**

Create `Todo.Web/Client/Services/ThemeService.cs`:

```csharp
using Microsoft.JSInterop;

namespace Todo.Web.Client.Services;

public enum Theme { Light, Dark }

public class ThemeService(IJSRuntime js)
{
    public Theme CurrentTheme { get; private set; } = Theme.Light;

    public async Task InitializeAsync()
    {
        try
        {
            var stored = await js.InvokeAsync<string>("todoTheme.getTheme");
            CurrentTheme = stored == "dark" ? Theme.Dark : Theme.Light;
        }
        catch
        {
            // localStorage unavailable (e.g. private browsing) — default to light
            CurrentTheme = Theme.Light;
        }
    }

    public async Task ToggleAsync()
    {
        CurrentTheme = CurrentTheme == Theme.Light ? Theme.Dark : Theme.Light;
        try
        {
            await js.InvokeVoidAsync("todoTheme.setTheme", CurrentTheme == Theme.Dark ? "dark" : "light");
        }
        catch
        {
            // Non-critical — UI still reflects the in-memory toggle
        }
    }
}
```

- [ ] **Step 2: Register ThemeService in `Program.cs`**

In `Todo.Web/Client/Program.cs`, replace the entire file:

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Todo.Web.Client;
using Todo.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient<TodoClient>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
});

builder.Services.AddScoped<ThemeService>();

await builder.Build().RunAsync();
```

- [ ] **Step 3: Add `using` to `_Imports.razor`**

In `Todo.Web/Client/_Imports.razor`, append at the end:

```razor
@using Todo.Web.Client.Services
```

- [ ] **Step 4: Build to verify no errors**

```bash
dotnet build Todo.Web/Client/Todo.Web.Client.csproj
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Todo.Web/Client/Services/ThemeService.cs Todo.Web/Client/Program.cs Todo.Web/Client/_Imports.razor
git commit -m "feat: add ThemeService for dark/light theme management

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

## Task 4: Add Theme Toggle Button to TodoApp.razor

**Files:**
- Modify: `Todo.Web/Client/TodoApp.razor`

- [ ] **Step 1: Replace the full content of `TodoApp.razor`**

The toggle button sits in a fixed top-right container, visible on both login and todo screens:

```razor
@inject TodoClient Client
@inject ThemeService ThemeService

<div style="position:fixed; top:0.75rem; right:1rem; z-index:1050;">
    <button class="btn btn-outline-secondary btn-sm" title="Toggle theme" @onclick="ToggleTheme">
        @(ThemeService.CurrentTheme == Theme.Dark ? "☀️" : "🌙")
    </button>
</div>

@if (!string.IsNullOrEmpty(CurrentUser))
{
    <ul class="nav justify-content-center">
        <li class="nav-item">
            Logged in as <strong>@CurrentUser</strong>
            <a class="btn btn-primary" role="button" @onclick="@Logout">Logout</a>
        </li>
    </ul>

    <TodoList OnForbidden="@Logout" />
}
else
{
    <LogInForm OnLoggedIn="@HandleLogin" SocialProviders="@SocialProviders" />
}

@code {
    [Parameter]
    public string? CurrentUser { get; set; }

    [Parameter]
    public string[] SocialProviders { get; set; } = Array.Empty<string>();

    protected override async Task OnInitializedAsync()
    {
        await ThemeService.InitializeAsync();
    }

    void HandleLogin(string newUsername)
    {
        CurrentUser = newUsername;
    }

    async Task Logout()
    {
        if (await Client.LogoutAsync())
        {
            CurrentUser = null;
        }
    }

    async Task ToggleTheme()
    {
        await ThemeService.ToggleAsync();
    }
}
```

- [ ] **Step 2: Build the full solution**

```bash
dotnet build TodoApp.sln
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Todo.Web/Client/TodoApp.razor
git commit -m "feat: add dark/light theme toggle button to TodoApp

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

## Task 5: Update app.css Focus Shadow

**Files:**
- Modify: `Todo.Web/Client/wwwroot/css/app.css`

Bootstrap 5.3 exposes `--bs-body-bg` which adapts automatically — replace the hardcoded `white`.

- [ ] **Step 1: Update focus box-shadow in `app.css`**

Replace:

```css
.btn:focus, .btn:active:focus, .btn-link.nav-link:focus, .form-control:focus, .form-check-input:focus {
    box-shadow: 0 0 0 0.1rem white, 0 0 0 0.25rem #258cfb;
}
```

With:

```css
.btn:focus, .btn:active:focus, .btn-link.nav-link:focus, .form-control:focus, .form-check-input:focus {
    box-shadow: 0 0 0 0.1rem var(--bs-body-bg), 0 0 0 0.25rem #258cfb;
}
```

- [ ] **Step 2: Commit**

```bash
git add Todo.Web/Client/wwwroot/css/app.css
git commit -m "fix: use CSS variable for focus shadow to adapt to dark theme

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

## Task 6: Run Tests and Manual Verification

- [ ] **Step 1: Run the full test suite**

```bash
dotnet test TodoApp.sln
```

Expected: all existing tests pass (API-layer tests, unaffected by frontend changes).

- [ ] **Step 2: Start the app via Aspire**

```bash
dotnet run --project TodoApp.AppHost
```

Or standalone web server only (no API calls, but enough to verify theming):

```bash
dotnet run --project "Todo.Web/Server/Todo.Web.Server.csproj"
```

- [ ] **Step 3: Verify toggle works**

1. Open the app in a browser
2. Click 🌙 — page switches to dark theme; button shows ☀️
3. Reload — dark theme is still active with no visible flash
4. Click ☀️ — page switches to light; button shows 🌙
5. Reload — light theme persists

- [ ] **Step 4: Verify both screens reflect theme**

- Toggle is visible and works on the **login screen**
- Log in; toggle is visible and works on the **todo list screen**

- [ ] **Step 5: Verify private browsing graceful fallback**

- Open in a private/incognito window
- Toggle — no errors in console; theme switches in-memory; app remains functional

- [ ] **Step 6: Push**

```bash
git push
```

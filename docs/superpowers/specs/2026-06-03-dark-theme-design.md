# Dark Theme — Design Spec

**Date:** 2026-06-03  
**Issue:** [#1 Dark theme](https://github.com/mvelikinac/TodoApp/issues/1)

## Summary

Add a toggleable dark/light theme to the Blazor WASM Todo App. The selected theme persists across sessions via `localStorage` and is applied immediately on page load to avoid a flash of the wrong theme.

---

## Approach

Bootstrap 5.1.0 (the current version) does not include native dark mode. Bootstrap 5.3+ introduced `data-bs-theme="dark"` as a first-class attribute on `<html>`, which toggles the entire Bootstrap color system — all components, utilities, and variables — with no custom CSS overrides needed.

**Chosen approach: Upgrade Bootstrap to 5.3.x and use `data-bs-theme`.**

Trade-offs considered:
- **CSS class + custom variables (no upgrade):** Works but requires manually overriding dozens of Bootstrap CSS variables. More fragile, more CSS to maintain.
- **`prefers-color-scheme` only (no toggle):** Simple but removes user control — the theme can't be overridden independently of the OS setting.
- **Upgrade to 5.3 + `data-bs-theme` (chosen):** Bootstrap handles all component theming. Minimal CSS additions. Future-proof. The only cost is replacing the bundled Bootstrap dist files.

---

## Architecture

### Bootstrap upgrade
Replace the Bootstrap 5.1.0 dist files in `Todo.Web/Client/wwwroot/bootstrap/dist/` with Bootstrap 5.3.x files. No Blazor package changes are needed — Bootstrap is served as static assets, not a NuGet package.

### ThemeService
A scoped Blazor service (`Todo.Web.Client`) responsible for:
- Reading the stored theme from `localStorage` via JS interop on initialization
- Writing the selected theme to `localStorage` on toggle
- Setting `data-bs-theme` on the `<html>` element via JS interop
- Exposing `CurrentTheme` (enum: `Light` / `Dark`) and a `ToggleAsync()` method

### JS interop (inline script)
A small `<script>` block in `App.razor`'s `<head>` (before CSS loads) reads `localStorage` and immediately sets `data-bs-theme` on `<html>`. This prevents the flash of the wrong theme on hard refresh. No separate `.js` file needed — the snippet is ~3 lines.

The same JS functions (`getTheme`, `setTheme`) are exposed on `window` for use by `ThemeService` via `IJSRuntime`.

### Toggle button
A sun/moon icon button added to `TodoApp.razor` in the top-right of the nav bar. Calls `ThemeService.ToggleAsync()` and re-renders. Visible on both the login screen and the todo list screen.

---

## Data Flow

```
Page load
  → inline <script> reads localStorage["theme"]
  → sets <html data-bs-theme="..."> immediately (no Blazor involved yet)

Blazor initializes
  → ThemeService.InitializeAsync() reads localStorage via IJSRuntime
  → sets CurrentTheme to match persisted value

User clicks toggle
  → ThemeService.ToggleAsync()
  → flips CurrentTheme
  → calls JS setTheme() → updates <html data-bs-theme="..."> + localStorage
  → component re-renders (icon updates)
```

---

## Components Changed

| File | Change |
|---|---|
| `App.razor` | Add inline `<script>` in `<head>` for flash prevention |
| `TodoApp.razor` | Add theme toggle button; inject `ThemeService` |
| `Todo.Web.Client/Program.cs` | Register `ThemeService` as scoped |
| `wwwroot/bootstrap/dist/` | Replace with Bootstrap 5.3.x dist files |
| `wwwroot/css/app.css` | Minor: update hardcoded colors to CSS variables if any remain |
| *(new)* `Services/ThemeService.cs` | New service in `Todo.Web.Client` |

---

## Error Handling

- If `localStorage` is unavailable (e.g., private browsing with storage blocked), JS interop will throw. `ThemeService.InitializeAsync()` wraps the JS call in a try/catch and silently falls back to `Light` theme.
- No async errors surface to the user — theme failure is non-critical.

---

## Testing

- Manual: verify toggle switches theme visually; verify preference survives page reload; verify login and todo screens both respect the theme.
- No automated tests are added — there are no existing frontend component tests in this repo and adding a test framework is out of scope.

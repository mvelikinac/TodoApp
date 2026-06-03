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

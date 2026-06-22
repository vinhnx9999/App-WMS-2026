namespace DP.AppWMS.Web.Services.State;

public class LayoutService
{
    public bool IsDarkMode { get; private set; }

    // Toggle between light and dark themes
    public event Action? OnThemeChanged;

    public void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        OnThemeChanged?.Invoke();
    }
}

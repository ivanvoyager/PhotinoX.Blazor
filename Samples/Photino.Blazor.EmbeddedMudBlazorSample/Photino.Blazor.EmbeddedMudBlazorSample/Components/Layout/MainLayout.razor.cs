using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Threading.Tasks;

namespace Photino.Blazor.EmbeddedMudBlazorSample.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase
    {
        private MudThemeProvider _mudThemeProvider = null!;
        private readonly MudTheme _mudTheme = AppThemeProvider.GetTheme();
        private bool _drawerOpen = true;
        private bool _isDarkMode;

        protected void DrawerToggle()
        {
            _drawerOpen = !_drawerOpen;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();

                await _mudThemeProvider.WatchSystemDarkModeAsync(OnSystemDarkModeChanged);

                StateHasChanged();
            }
        }

        private Task OnSystemDarkModeChanged(bool darkMode)
        {
            _isDarkMode = darkMode;
            StateHasChanged();
            return Task.CompletedTask;
        }
    }
}

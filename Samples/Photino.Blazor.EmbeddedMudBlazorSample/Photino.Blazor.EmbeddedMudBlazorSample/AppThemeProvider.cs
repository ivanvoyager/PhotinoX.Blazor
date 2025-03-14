using MudBlazor;

namespace Photino.Blazor.EmbeddedMudBlazorSample
{
    public class AppThemeProvider
    {
        public static MudTheme GetTheme()
        {
            var theme = new MudTheme();

			theme.PaletteLight.Primary = "#1E5078";
			theme.PaletteLight.Secondary = "#4b7393";
            theme.PaletteLight.AppbarBackground = theme.PaletteDark.Background;

			theme.PaletteDark.Primary = "#1E5078";
			theme.PaletteDark.Secondary = "#4b7393";

			return theme;
        }
    }
}

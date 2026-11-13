using EssayOrganizer.App.Models;

namespace EssayOrganizer.App.Services;

public interface ISettingsService
{
    Task<FontSettings> GetFontSettingsAsync();
    Task SaveFontSettingsAsync(FontSettings settings);
    Task<string> GetThemeAsync();
    Task SaveThemeAsync(string theme);
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
}


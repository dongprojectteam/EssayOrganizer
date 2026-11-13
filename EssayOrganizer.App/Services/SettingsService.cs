using EssayOrganizer.App.Data;
using EssayOrganizer.App.Models;
using Microsoft.EntityFrameworkCore;

namespace EssayOrganizer.App.Services;

public class SettingsService : ISettingsService
{
    public const string BodyFontSizeKey = "ui.body_font_size";
    public const string LastAutoBackupDateKey = "backup.last_auto_date";
    public const string AppThemeKey = "ui.theme";

    private readonly EssayDbContextFactory _factory;

    public SettingsService(EssayDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> GetThemeAsync()
    {
        var value = await GetAsync(AppThemeKey);
        return value is "Dark" or "Light" ? value : "Light";
    }

    public async Task SaveThemeAsync(string theme)
    {
        await SetAsync(AppThemeKey, theme);
    }

    public async Task<FontSettings> GetFontSettingsAsync()
    {
        var value = await GetAsync(BodyFontSizeKey);
        var settings = new FontSettings();

        if (double.TryParse(value, out var fontSize))
        {
            settings.BodyFontSize = fontSize;
        }

        settings.Clamp();
        return settings;
    }

    public async Task SaveFontSettingsAsync(FontSettings settings)
    {
        settings.Clamp();
        await SetAsync(BodyFontSizeKey, settings.BodyFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public async Task<string?> GetAsync(string key)
    {
        await using var db = _factory.CreateDbContext();
        var row = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == key);
        return row?.Value;
    }

    public async Task SetAsync(string key, string value)
    {
        await using var db = _factory.CreateDbContext();
        var row = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == key);

        if (row is null)
        {
            db.AppSettings.Add(new Models.AppSetting { Key = key, Value = value });
        }
        else
        {
            row.Value = value;
            db.AppSettings.Update(row);
        }

        await db.SaveChangesAsync();
    }
}


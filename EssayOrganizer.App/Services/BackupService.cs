using System.IO;
using EssayOrganizer.App.Data;

namespace EssayOrganizer.App.Services;

public class BackupService : IBackupService
{
    private readonly AppPaths _paths;
    private readonly ISettingsService _settingsService;

    public BackupService(AppPaths paths, ISettingsService settingsService)
    {
        _paths = paths;
        _settingsService = settingsService;
    }

    public Task<string> CreateBackupAsync()
    {
        var fileName = $"essay-organizer-{DateTime.Now:yyyyMMdd-HHmmss}.db";
        var targetPath = Path.Combine(_paths.BackupDirectory, fileName);
        File.Copy(_paths.DatabasePath, targetPath, overwrite: true);
        return Task.FromResult(targetPath);
    }

    public Task RestoreBackupAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("백업 파일을 찾을 수 없습니다.", filePath);
        }

        var tempPath = _paths.DatabasePath + ".restore.tmp";
        File.Copy(filePath, tempPath, overwrite: true);
        File.Copy(tempPath, _paths.DatabasePath, overwrite: true);
        File.Delete(tempPath);
        return Task.CompletedTask;
    }

    public async Task RunAutoBackupIfNeededAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var lastDate = await _settingsService.GetAsync(SettingsService.LastAutoBackupDateKey);

        if (DateOnly.TryParse(lastDate, out var parsed) && parsed == today)
        {
            return;
        }

        await CreateBackupAsync();
        await _settingsService.SetAsync(SettingsService.LastAutoBackupDateKey, today.ToString("yyyy-MM-dd"));

        var files = Directory.GetFiles(_paths.BackupDirectory, "essay-organizer-*.db")
            .Select(path => new FileInfo(path))
            .OrderByDescending(x => x.CreationTimeUtc)
            .ToList();

        foreach (var old in files.Skip(14))
        {
            old.Delete();
        }
    }
}



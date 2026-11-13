namespace EssayOrganizer.App.Services;

public interface IBackupService
{
    Task<string> CreateBackupAsync();
    Task RestoreBackupAsync(string filePath);
    Task RunAutoBackupIfNeededAsync();
}


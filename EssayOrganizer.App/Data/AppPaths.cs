using System.IO;

namespace EssayOrganizer.App.Data;

public class AppPaths
{
    public required string DataDirectory { get; init; }
    public required string BackupDirectory { get; init; }
    public required string DatabasePath { get; init; }
    public required string ProfileImagesDirectory { get; init; }

    public static AppPaths CreateDefault()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EssayOrganizer");
        return new AppPaths
        {
            DataDirectory = root,
            BackupDirectory = Path.Combine(root, "Backups"),
            DatabasePath = Path.Combine(root, "essay-organizer.db"),
            ProfileImagesDirectory = Path.Combine(root, "Profiles")
        };
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(BackupDirectory);
        Directory.CreateDirectory(ProfileImagesDirectory);
    }
}



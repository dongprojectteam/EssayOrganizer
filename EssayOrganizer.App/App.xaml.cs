using System.IO;
using System.Windows;
using EssayOrganizer.App.Data;
using EssayOrganizer.App.Services;
using EssayOrganizer.App.ViewModels;
using EssayOrganizer.App.Views;

namespace EssayOrganizer.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // The profile picker is shown before MainWindow exists. Prevent its modal
        // lifetime from triggering WPF's "last window closed" shutdown behavior.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            var paths = AppPaths.CreateDefault();
            paths.EnsureDirectories();

            var dbFactory = new EssayDbContextFactory(paths.DatabasePath);
            await DatabaseInitializer.InitializeAsync(dbFactory);

            var repository = new EssayRepository(dbFactory);
            var profileRepository = new ProfileRepository(dbFactory);
            var settingsService = new SettingsService(dbFactory);
            var statsService = new StatsService();
            var backupService = new BackupService(paths, settingsService);
            var printService = new PrintService();

            await backupService.RunAutoBackupIfNeededAsync();

            var savedTheme = await settingsService.GetThemeAsync();
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
                savedTheme == "Dark" ? Wpf.Ui.Appearance.ApplicationTheme.Dark : Wpf.Ui.Appearance.ApplicationTheme.Light,
                Wpf.Ui.Controls.WindowBackdropType.Mica,
                updateAccent: true);

            var profileWindow = new ProfileSelectionWindow(profileRepository);
            if (profileWindow.ShowDialog() != true || profileWindow.SelectedProfile is null)
            {
                Shutdown();
                return;
            }

            var mainViewModel = new MainViewModel(repository, statsService, settingsService, backupService, printService, profileRepository, profileWindow.SelectedProfile.Id);
            await mainViewModel.InitializeAsync();

            var window = new MainWindow(
                mainViewModel,
                profileWindow.SelectedProfile,
                settingsService,
                profileRepository,
                repository,
                statsService,
                backupService,
                printService);
            MainWindow = window;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
        }
        catch (Exception ex)
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EssayOrganizer");
            Directory.CreateDirectory(appData);
            var logPath = Path.Combine(appData, "startup-error.log");
            File.WriteAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n{ex}");

            MessageBox.Show(
                $"앱 시작 중 오류가 발생했습니다.\n\n{ex.Message}\n\n로그: {logPath}",
                "시작 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }
}

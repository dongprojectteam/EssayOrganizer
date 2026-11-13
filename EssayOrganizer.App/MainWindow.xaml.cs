using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using EssayOrganizer.App.Utils;
using EssayOrganizer.App.ViewModels;
using EssayOrganizer.App.Models;
using EssayOrganizer.App.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace EssayOrganizer.App;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private readonly IProfileRepository _profileRepository;
    private readonly IEssayRepository _repository;
    private readonly IStatsService _statsService;
    private readonly IBackupService _backupService;
    private readonly IPrintService _printService;
    private bool _isLoadingRichText;

    public Profile CurrentProfile { get; }
    public string AppVersionText { get; }

    public MainWindow(
        MainViewModel viewModel,
        Profile profile,
        ISettingsService settingsService,
        IProfileRepository profileRepository,
        IEssayRepository repository,
        IStatsService statsService,
        IBackupService backupService,
        IPrintService printService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        CurrentProfile = profile;
        _settingsService = settingsService;
        _profileRepository = profileRepository;
        _repository = repository;
        _statsService = statsService;
        _backupService = backupService;
        _printService = printService;

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        AppVersionText = version is not null ? $"v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}" : "v1.1.0";
        DataContext = viewModel;
        Title = $"논술 정리 · {profile.Name} ({AppVersionText})";

        StartupToggle.IsChecked = StartupService.IsStartupEnabled();
        StartupToggle.Checked += StartupToggle_Checked;
        StartupToggle.Unchecked += StartupToggle_Unchecked;

        viewModel.Editor.RichTextLoadRequested += LoadRichTextDocument;
        viewModel.Reader.RichTextLoadRequested += LoadReaderDocument;
        LoadRichTextDocument();
        LoadReaderDocument();
    }

    private void StartupToggle_Checked(object sender, RoutedEventArgs e)
    {
        StartupService.SetStartup(true);
    }

    private void StartupToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        StartupService.SetStartup(false);
    }

    private async void SwitchProfile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Views.ProfileSelectionWindow(_profileRepository) { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedProfile is null || picker.SelectedProfile.Id == CurrentProfile.Id)
        {
            return;
        }

        var newProfile = picker.SelectedProfile;
        var newViewModel = new MainViewModel(_repository, _statsService, _settingsService, _backupService, _printService, _profileRepository, newProfile.Id);
        await newViewModel.InitializeAsync();

        var newWindow = new MainWindow(
            newViewModel,
            newProfile,
            _settingsService,
            _profileRepository,
            _repository,
            _statsService,
            _backupService,
            _printService);

        Application.Current.MainWindow = newWindow;
        newWindow.Show();
        Close();
    }

    private void RichEditor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isLoadingRichText || _viewModel is null) return;

        var range = new TextRange(RichEditor.Document.ContentStart, RichEditor.Document.ContentEnd);
        _viewModel.Editor.Body = range.Text.TrimEnd('\r', '\n');

        _isLoadingRichText = true;
        try
        {
            EssayDocumentAppearance.PrepareForStorage(RichEditor.Document);
            using (var stream = new MemoryStream())
            {
                range.Save(stream, DataFormats.Rtf);
                _viewModel.Editor.FormattedBody = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }

            EssayDocumentAppearance.ApplyDisplayTheme(RichEditor.Document);
        }
        finally
        {
            _isLoadingRichText = false;
        }
    }

    private void LoadRichTextDocument()
    {
        if (!IsInitialized) return;
        _isLoadingRichText = true;
        try
        {
            RichEditor.Document.Blocks.Clear();
            var formatted = _viewModel.Editor.FormattedBody;
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(formatted));
                new TextRange(RichEditor.Document.ContentStart, RichEditor.Document.ContentEnd).Load(stream, DataFormats.Rtf);
                EssayDocumentAppearance.ApplyLegacyFormattedBodyPolicy(RichEditor.Document);
            }
            else
            {
                AddPlainTextBlocks(RichEditor.Document, _viewModel.Editor.Body);
                EssayDocumentAppearance.ApplyDisplayTheme(RichEditor.Document);
            }
        }
        catch (ArgumentException)
        {
            RichEditor.Document.Blocks.Clear();
            AddPlainTextBlocks(RichEditor.Document, _viewModel.Editor.Body);
            EssayDocumentAppearance.ApplyDisplayTheme(RichEditor.Document);
        }
        finally
        {
            _isLoadingRichText = false;
        }
    }

    private void LoadReaderDocument()
    {
        if (!IsInitialized) return;
        LoadFormattedDocument(RichReader, _viewModel.Reader.FormattedBody, _viewModel.Reader.Body);
    }

    private static void LoadFormattedDocument(System.Windows.Controls.RichTextBox target, string formattedBody, string plainBody)
    {
        target.Document.Blocks.Clear();
        try
        {
            if (!string.IsNullOrWhiteSpace(formattedBody))
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(formattedBody));
                new TextRange(target.Document.ContentStart, target.Document.ContentEnd).Load(stream, DataFormats.Rtf);
                EssayDocumentAppearance.ApplyLegacyFormattedBodyPolicy(target.Document);
                return;
            }
        }
        catch (ArgumentException)
        {
            // Use the plain text below if the legacy formatted value is invalid.
        }
        AddPlainTextBlocks(target.Document, plainBody);
        EssayDocumentAppearance.ApplyDisplayTheme(target.Document);
    }

    private static void AddPlainTextBlocks(FlowDocument document, string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (var line in lines)
            document.Blocks.Add(new Paragraph(new Run(line)));
    }

    private void Highlight_Click(object sender, RoutedEventArgs e)
    {
        if (RichEditor.Selection.IsEmpty) return;
        var isDark = EssayDocumentAppearance.IsDarkTheme();
        RichEditor.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, EssayDocumentAppearance.HighlightBrush(isDark));
        RichEditor.Focus();
    }

    private void ClearHighlight_Click(object sender, RoutedEventArgs e)
    {
        if (RichEditor.Selection.IsEmpty) return;
        RichEditor.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
        RichEditor.Focus();
    }

    private async void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        var currentTheme = ApplicationThemeManager.GetAppTheme();
        var newTheme = currentTheme == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark;
        ApplicationThemeManager.Apply(newTheme, WindowBackdropType.Mica, updateAccent: true);
        await _settingsService.SaveThemeAsync(newTheme == ApplicationTheme.Dark ? "Dark" : "Light");

        EssayDocumentAppearance.ApplyDisplayTheme(RichEditor.Document);
        EssayDocumentAppearance.ApplyDisplayTheme(RichReader.Document);
    }

    private void InsertSymbol_Click(object sender, RoutedEventArgs e)
    {
        string? symbol = null;
        if (sender is FrameworkElement fe && fe.Tag is string tag)
        {
            symbol = tag;
        }
        else if (sender is System.Windows.Controls.MenuItem mi && mi.Tag is string menuTag)
        {
            symbol = menuTag;
        }

        if (string.IsNullOrEmpty(symbol)) return;

        InsertTextIntoEditor(symbol);

        if (CircleNumberPopup is not null && CircleNumberPopup.IsOpen)
        {
            CircleNumberPopup.IsOpen = false;
        }
    }

    private void InsertTextIntoEditor(string text)
    {
        if (RichEditor.CaretPosition is null)
        {
            RichEditor.CaretPosition = RichEditor.Document.ContentEnd;
        }

        if (text is "「」" or "『』" or "《》" or "【】")
        {
            var selection = RichEditor.Selection;
            if (!selection.IsEmpty)
            {
                var inner = selection.Text;
                var open = text[..1];
                var close = text[1..];
                selection.Text = $"{open}{inner}{close}";
                RichEditor.Focus();
                return;
            }

            selection.Text = text;
            var prevPos = selection.End.GetPositionAtOffset(-1);
            if (prevPos is not null)
            {
                RichEditor.CaretPosition = prevPos;
            }
            RichEditor.Focus();
            return;
        }

        RichEditor.Selection.Text = text;
        RichEditor.CaretPosition = RichEditor.Selection.End;
        RichEditor.Focus();
    }

    private void CircleNumberButton_Click(object sender, RoutedEventArgs e)
    {
        if (CircleNumberPopup is not null)
        {
            CircleNumberPopup.IsOpen = !CircleNumberPopup.IsOpen;
        }
    }

    private void SymbolMenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.ContextMenu is not null)
        {
            fe.ContextMenu.PlacementTarget = fe;
            fe.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            fe.ContextMenu.IsOpen = true;
        }
    }
}

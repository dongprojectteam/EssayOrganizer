using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EssayOrganizer.App.Models;
using EssayOrganizer.App.Services;
using EssayOrganizer.App.Utils;
using EssayOrganizer.App.Views;
using Microsoft.Win32;

namespace EssayOrganizer.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IEssayRepository _repository;
    private readonly IStatsService _statsService;
    private readonly ISettingsService _settingsService;
    private readonly IBackupService _backupService;
    private readonly IPrintService _printService;
    private readonly IProfileRepository _profileRepository;
    private readonly Guid _profileId;

    private readonly DispatcherDebouncer _saveDebouncer = new(TimeSpan.FromMilliseconds(700));

    private Essay? _currentEssay;
    private bool _isApplyingEditorState;
    private bool _ignoreSidebarSelectionChanged;

    public SidebarViewModel Sidebar { get; } = new();
    public EssayReaderViewModel Reader { get; } = new();
    public EssayEditorViewModel Editor { get; } = new();
    public StatusBarViewModel Status { get; } = new();

    [ObservableProperty]
    private EditorMode mode = EditorMode.Empty;

    [ObservableProperty]
    private double bodyFontSize = FontSettings.DefaultSize;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool IsReadMode => Mode == EditorMode.Read;
    public bool IsEditMode => Mode == EditorMode.Edit;
    public bool IsEmptyMode => Mode == EditorMode.Empty;

    public string EditorHeaderText => Editor.IsNewEssay ? "새 논술 작성" : "논술 편집";

    public IAsyncRelayCommand CreateNewEssayCommand { get; }
    public IAsyncRelayCommand EditEssayCommand { get; }
    public IAsyncRelayCommand DeleteEssayCommand { get; }
    public IAsyncRelayCommand MoveEssayCommand { get; }
    public IRelayCommand PrintEssayCommand { get; }
    public IAsyncRelayCommand CreateBackupCommand { get; }
    public IAsyncRelayCommand RestoreBackupCommand { get; }
    public IAsyncRelayCommand DecreaseFontCommand { get; }
    public IAsyncRelayCommand IncreaseFontCommand { get; }
    public IAsyncRelayCommand ResetFontCommand { get; }
    public IRelayCommand BackToReadModeCommand { get; }

    public MainViewModel(
        IEssayRepository repository,
        IStatsService statsService,
        ISettingsService settingsService,
        IBackupService backupService,
        IPrintService printService,
        IProfileRepository profileRepository,
        Guid profileId)
    {
        _repository = repository;
        _statsService = statsService;
        _settingsService = settingsService;
        _backupService = backupService;
        _printService = printService;
        _profileRepository = profileRepository;
        _profileId = profileId;

        Sidebar.SearchRequested += LoadEssaysAsync;
        Sidebar.EssaySelected += OnSidebarEssaySelected;
        Editor.ContentChanged += OnEditorContentChanged;

        CreateNewEssayCommand = new AsyncRelayCommand(CreateNewEssayAsync);
        EditEssayCommand = new AsyncRelayCommand(EditEssayAsync);
        DeleteEssayCommand = new AsyncRelayCommand(DeleteEssayAsync);
        MoveEssayCommand = new AsyncRelayCommand(MoveEssayAsync);
        PrintEssayCommand = new RelayCommand(PrintEssay);
        CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync);
        RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync);
        DecreaseFontCommand = new AsyncRelayCommand(async () => await ChangeFontAsync(-1));
        IncreaseFontCommand = new AsyncRelayCommand(async () => await ChangeFontAsync(1));
        ResetFontCommand = new AsyncRelayCommand(ResetFontAsync);
        BackToReadModeCommand = new RelayCommand(BackToReadMode);
    }

    public async Task InitializeAsync()
    {
        var fontSettings = await _settingsService.GetFontSettingsAsync();
        BodyFontSize = fontSettings.BodyFontSize;

        await LoadEssaysAsync(string.Empty);

        if (Sidebar.EssayItems.Any())
        {
            var firstId = Sidebar.EssayItems[0].Id;
            await LoadEssayForReadModeAsync(firstId);
            SetSidebarSelectionWithoutEvents(Sidebar.EssayItems[0]);
        }
        else
        {
            SetEmptyState();
        }
    }

    partial void OnModeChanged(EditorMode value)
    {
        OnPropertyChanged(nameof(IsReadMode));
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(IsEmptyMode));
        OnPropertyChanged(nameof(EditorHeaderText));
    }

    partial void OnBodyFontSizeChanged(double value)
    {
        if (value < FontSettings.MinSize || value > FontSettings.MaxSize)
        {
            BodyFontSize = Math.Clamp(value, FontSettings.MinSize, FontSettings.MaxSize);
        }
    }

    private async Task LoadEssaysAsync(string query)
    {
        try
        {
            var essays = await _repository.SearchAsync(_profileId, query);
            Sidebar.SetEssays(essays);
            ErrorMessage = string.Empty;

            if (_currentEssay is not null)
            {
                SetSidebarSelectionWithoutEvents(Sidebar.EssayItems.FirstOrDefault(x => x.Id == _currentEssay.Id));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            MessageBox.Show($"목록 조회 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadEssayForReadModeAsync(Guid id)
    {
        try
        {
            var essay = await _repository.GetByIdAsync(_profileId, id);
            if (essay is null)
            {
                SetEmptyState();
                return;
            }

            _currentEssay = essay;
            Reader.Load(essay);
            RefreshStats(essay.Body);
            Mode = EditorMode.Read;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            MessageBox.Show($"논술을 여는 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Task CreateNewEssayAsync()
    {
        var now = DateTime.UtcNow;
        _currentEssay = new Essay
        {
            Id = Guid.NewGuid(),
            ProfileId = _profileId,
            Title = string.Empty,
            Body = string.Empty,
            FormattedBody = string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };

        _isApplyingEditorState = true;
        Editor.LoadFrom(_currentEssay, isNew: true);
        _isApplyingEditorState = false;

        RefreshStats(string.Empty);
        Mode = EditorMode.Edit;
        return Task.CompletedTask;
    }

    private Task EditEssayAsync()
    {
        if (_currentEssay is null)
        {
            return Task.CompletedTask;
        }

        _isApplyingEditorState = true;
        Editor.LoadFrom(_currentEssay, isNew: false);
        _isApplyingEditorState = false;

        RefreshStats(Editor.Body);
        Mode = EditorMode.Edit;
        return Task.CompletedTask;
    }

    private async Task DeleteEssayAsync()
    {
        if (_currentEssay is null)
        {
            return;
        }

        var confirm = MessageBox.Show("이 논술을 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _repository.DeleteAsync(_profileId, _currentEssay.Id);
            _currentEssay = null;

            await LoadEssaysAsync(Sidebar.SearchQuery);

            if (Sidebar.EssayItems.Any())
            {
                SetSidebarSelectionWithoutEvents(Sidebar.EssayItems[0]);
                await LoadEssayForReadModeAsync(Sidebar.EssayItems[0].Id);
            }
            else
            {
                SetEmptyState();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"삭제 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task MoveEssayAsync()
    {
        if (_currentEssay is null) return;

        var picker = new ProfileSelectionWindow(_profileRepository) { Owner = Application.Current.MainWindow };
        if (picker.ShowDialog() != true || picker.SelectedProfile is null || picker.SelectedProfile.Id == _profileId) return;

        try
        {
            await _repository.MoveAsync(_currentEssay.Id, _profileId, picker.SelectedProfile.Id);
            MessageBox.Show($"‘{_currentEssay.Title}’을(를) {picker.SelectedProfile.Name} 프로파일로 옮겼습니다.", "프로파일 이동", MessageBoxButton.OK, MessageBoxImage.Information);
            SetEmptyState();
            await LoadEssaysAsync(Sidebar.SearchQuery);
            if (Sidebar.EssayItems.Any())
            {
                SetSidebarSelectionWithoutEvents(Sidebar.EssayItems[0]);
                await LoadEssayForReadModeAsync(Sidebar.EssayItems[0].Id);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"프로파일 이동 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrintEssay()
    {
        if (_currentEssay is null)
        {
            return;
        }

        try
        {
            _printService.Print(_currentEssay, new FontSettings { BodyFontSize = BodyFontSize });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"출력 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task CreateBackupAsync()
    {
        try
        {
            var path = await _backupService.CreateBackupAsync();
            MessageBox.Show($"백업을 생성했습니다.\n{path}", "백업 완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"백업 생성 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RestoreBackupAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "복원할 백업 파일 선택",
            Filter = "DB 파일 (*.db)|*.db|모든 파일 (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _backupService.RestoreBackupAsync(dialog.FileName);
            MessageBox.Show("복원이 완료되었습니다. 앱을 다시 시작하면 최신 상태가 반영됩니다.", "복원 완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"복원 중 오류가 발생했습니다. 원본 데이터는 유지되었습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ChangeFontAsync(double delta)
    {
        var updated = Math.Clamp(BodyFontSize + delta, FontSettings.MinSize, FontSettings.MaxSize);
        await SaveFontSizeAsync(updated);
    }

    private async Task ResetFontAsync()
    {
        await SaveFontSizeAsync(FontSettings.DefaultSize);
    }

    private async Task SaveFontSizeAsync(double size)
    {
        BodyFontSize = size;
        await _settingsService.SaveFontSettingsAsync(new FontSettings { BodyFontSize = size });
    }

    private void BackToReadMode()
    {
        if (_currentEssay is null)
        {
            SetEmptyState();
            return;
        }

        Reader.Load(_currentEssay);
        RefreshStats(_currentEssay.Body);
        Mode = EditorMode.Read;
    }

    private void OnEditorContentChanged()
    {
        if (_isApplyingEditorState)
        {
            return;
        }

        RefreshStats(Editor.Body);
        _saveDebouncer.Debounce(() =>
        {
            _ = SaveEditorAsync();
        });
    }

    private async Task SaveEditorAsync()
    {
        if (_currentEssay is null)
        {
            return;
        }

        try
        {
            _currentEssay.Title = BuildSafeTitle(Editor.Title);
            _currentEssay.Body = Editor.Body;
            _currentEssay.FormattedBody = Editor.FormattedBody;
            _currentEssay.UpdatedAt = DateTime.UtcNow;

            if (Editor.IsNewEssay)
            {
                _currentEssay.CreatedAt = DateTime.UtcNow;
                await _repository.AddAsync(_currentEssay);
                Editor.IsNewEssay = false;
            }
            else
            {
                await _repository.UpdateAsync(_currentEssay);
            }

            Reader.Load(_currentEssay);
            Editor.SetSaved(DateTime.Now);
            Status.LastSavedText = $"마지막 저장: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            Sidebar.UpsertEssay(_currentEssay);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"저장 중 오류가 발생했습니다. 재시도해 주세요.\n{ex.Message}", "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshStats(string body)
    {
        var stats = _statsService.Calculate(body);
        Status.CharacterCount = stats.CharacterCount;
        Status.CharacterCountWithoutSpaces = stats.CharacterCountWithoutSpaces;
        Status.LineCount = stats.LineCount;
        Status.ParagraphCount = stats.ParagraphCount;
    }

    private void SetEmptyState()
    {
        _currentEssay = null;
        Reader.Clear();
        Editor.Clear();
        RefreshStats(string.Empty);
        Mode = EditorMode.Empty;
    }

    private static string BuildSafeTitle(string title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            return trimmed.Length <= 200 ? trimmed : trimmed[..200];
        }

        return $"제목 없음 ({DateTime.Now:yyyy-MM-dd HH:mm})";
    }

    private async void OnSidebarEssaySelected(Guid id)
    {
        if (_ignoreSidebarSelectionChanged)
        {
            return;
        }

        await LoadEssayForReadModeAsync(id);
    }

    private void SetSidebarSelectionWithoutEvents(EssayListItemViewModel? item)
    {
        _ignoreSidebarSelectionChanged = true;
        try
        {
            Sidebar.SelectedEssay = item;
        }
        finally
        {
            _ignoreSidebarSelectionChanged = false;
        }
    }
}

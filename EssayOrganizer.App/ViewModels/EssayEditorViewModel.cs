using CommunityToolkit.Mvvm.ComponentModel;

namespace EssayOrganizer.App.ViewModels;

public partial class EssayEditorViewModel : ObservableObject
{
    public event Action? ContentChanged;
    public event Action? RichTextLoadRequested;

    [ObservableProperty]
    private Guid essayId;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string body = string.Empty;

    [ObservableProperty]
    private string formattedBody = string.Empty;

    [ObservableProperty]
    private bool isNewEssay;

    [ObservableProperty]
    private string saveStatus = "저장됨";

    partial void OnTitleChanged(string value)
    {
        SaveStatus = "저장 중...";
        ContentChanged?.Invoke();
    }

    partial void OnBodyChanged(string value)
    {
        SaveStatus = "저장 중...";
        ContentChanged?.Invoke();
    }

    partial void OnFormattedBodyChanged(string value)
    {
        SaveStatus = "저장 중...";
        ContentChanged?.Invoke();
    }

    public void SetSaved(DateTime when)
    {
        SaveStatus = $"자동저장 완료 ({when:HH:mm:ss})";
    }

    public void LoadFrom(Models.Essay essay, bool isNew)
    {
        EssayId = essay.Id;
        IsNewEssay = isNew;
        Title = essay.Title;
        Body = essay.Body;
        FormattedBody = essay.FormattedBody;
        SaveStatus = "저장됨";
        RichTextLoadRequested?.Invoke();
    }

    public void Clear()
    {
        EssayId = Guid.Empty;
        Title = string.Empty;
        Body = string.Empty;
        FormattedBody = string.Empty;
        IsNewEssay = false;
        SaveStatus = "저장됨";
        RichTextLoadRequested?.Invoke();
    }
}


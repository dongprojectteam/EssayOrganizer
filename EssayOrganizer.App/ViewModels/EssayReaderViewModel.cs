using CommunityToolkit.Mvvm.ComponentModel;
using EssayOrganizer.App.Models;

namespace EssayOrganizer.App.ViewModels;

public partial class EssayReaderViewModel : ObservableObject
{
    public event Action? RichTextLoadRequested;
    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string body = string.Empty;

    [ObservableProperty]
    private string formattedBody = string.Empty;

    [ObservableProperty]
    private DateTime? updatedAt;

    [ObservableProperty]
    private Guid essayId;

    public void Load(Essay essay)
    {
        EssayId = essay.Id;
        Title = essay.Title;
        Body = essay.Body;
        FormattedBody = essay.FormattedBody;
        UpdatedAt = essay.UpdatedAt;
        RichTextLoadRequested?.Invoke();
    }

    public void Clear()
    {
        EssayId = Guid.Empty;
        Title = string.Empty;
        Body = string.Empty;
        FormattedBody = string.Empty;
        UpdatedAt = null;
        RichTextLoadRequested?.Invoke();
    }
}


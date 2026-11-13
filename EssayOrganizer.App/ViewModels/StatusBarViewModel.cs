using CommunityToolkit.Mvvm.ComponentModel;

namespace EssayOrganizer.App.ViewModels;

public partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    private int characterCount;

    [ObservableProperty]
    private int characterCountWithoutSpaces;

    [ObservableProperty]
    private int lineCount;

    [ObservableProperty]
    private int paragraphCount;

    [ObservableProperty]
    private string lastSavedText = "마지막 저장: -";
}


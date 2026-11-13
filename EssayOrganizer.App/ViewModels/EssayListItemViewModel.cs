using CommunityToolkit.Mvvm.ComponentModel;

namespace EssayOrganizer.App.ViewModels;

public partial class EssayListItemViewModel : ObservableObject
{
    public Guid Id { get; init; }

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string preview = string.Empty;

    [ObservableProperty]
    private DateTime updatedAt;

    public string GroupKey => UpdatedAt.ToLocalTime().ToString("yyyy-MM");

    public string UpdatedDisplay => UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}


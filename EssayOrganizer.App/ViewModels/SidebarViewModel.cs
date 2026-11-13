using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using EssayOrganizer.App.Models;
using EssayOrganizer.App.Utils;

namespace EssayOrganizer.App.ViewModels;

public partial class SidebarViewModel : ObservableObject
{
    private readonly DispatcherDebouncer _searchDebouncer = new(TimeSpan.FromMilliseconds(150));

    public event Func<string, Task>? SearchRequested;
    public event Action<Guid>? EssaySelected;

    public ObservableCollection<EssayListItemViewModel> EssayItems { get; } = [];

    public ICollectionView EssaysView { get; }

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private EssayListItemViewModel? selectedEssay;

    public SidebarViewModel()
    {
        EssaysView = CollectionViewSource.GetDefaultView(EssayItems);
        EssaysView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(EssayListItemViewModel.GroupKey)));
    }

    partial void OnSearchQueryChanged(string value)
    {
        _searchDebouncer.Debounce(() =>
        {
            _ = SearchRequested?.Invoke(value);
        });
    }

    partial void OnSelectedEssayChanged(EssayListItemViewModel? value)
    {
        if (value is not null)
        {
            EssaySelected?.Invoke(value.Id);
        }
    }

    public void SetEssays(IEnumerable<Essay> essays)
    {
        EssayItems.Clear();

        foreach (var essay in essays.OrderByDescending(x => x.UpdatedAt))
        {
            EssayItems.Add(new EssayListItemViewModel
            {
                Id = essay.Id,
                Title = essay.Title,
                Preview = BuildPreview(essay.Body),
                UpdatedAt = essay.UpdatedAt
            });
        }

        EssaysView.Refresh();
    }

    public void UpsertEssay(Essay essay)
    {
        var existing = EssayItems.FirstOrDefault(x => x.Id == essay.Id);

        if (existing is null)
        {
            var newItem = new EssayListItemViewModel
            {
                Id = essay.Id,
                Title = essay.Title,
                Preview = BuildPreview(essay.Body),
                UpdatedAt = essay.UpdatedAt
            };

            InsertInSortedOrder(newItem);
        }
        else
        {
            // 현재 선택 상태를 유지하면서 내용과 위치만 갱신
            existing.Title = essay.Title;
            existing.Preview = BuildPreview(essay.Body);
            existing.UpdatedAt = essay.UpdatedAt;

            // UpdatedAt이 바뀌었을 수 있으므로 정렬 순서 재조정
            var currentIndex = EssayItems.IndexOf(existing);
            EssayItems.RemoveAt(currentIndex);
            InsertInSortedOrder(existing);
        }

        EssaysView.Refresh();
    }

    private void InsertInSortedOrder(EssayListItemViewModel item)
    {
        var index = 0;
        while (index < EssayItems.Count && EssayItems[index].UpdatedAt > item.UpdatedAt)
        {
            index++;
        }

        EssayItems.Insert(index, item);
    }

    private static string BuildPreview(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        var oneLine = body.Replace("\r", " ").Replace("\n", " ").Trim();
        var width = 0;
        const int maxWidth = 38; // 한글 기준 약 19자, 영문 기준 38자

        for (int i = 0; i < oneLine.Length; i++)
        {
            var c = oneLine[i];
            width += (c >= 0xAC00 && c <= 0xD7A3) || c > 127 ? 2 : 1;
            if (width > maxWidth)
            {
                return oneLine[..i].TrimEnd() + "...";
            }
        }

        return oneLine;
    }
}



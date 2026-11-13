using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EssayOrganizer.App.Models;
using EssayOrganizer.App.Services;
using Wpf.Ui.Controls;

namespace EssayOrganizer.App.Views;

public partial class ProfileSelectionWindow : FluentWindow
{
    private readonly IProfileRepository _profiles;
    public Profile? SelectedProfile { get; private set; }
    public ObservableCollection<ProfileCard> Cards { get; } = new();

    public ProfileSelectionWindow(IProfileRepository profiles)
    {
        _profiles = profiles;
        InitializeComponent();
        ProfilesList.ItemsSource = Cards;
        Loaded += async (_, _) => await LoadProfilesAsync();
    }

    private async Task LoadProfilesAsync()
    {
        Cards.Clear();
        foreach (var profile in await _profiles.GetAllAsync())
            Cards.Add(new ProfileCard(profile));

        if (Cards.Count > 0 && ProfilesList.SelectedItem is null)
        {
            ProfilesList.SelectedIndex = 0;
        }
    }

    private void ProfilesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var hasSelection = ProfilesList.SelectedItem is ProfileCard;
        SelectButton.IsEnabled = hasSelection;
        EditButton.IsEnabled = hasSelection;
    }

    private void ProfilesList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ProfilesList.SelectedItem is ProfileCard)
        {
            SelectProfile_Click(sender, e);
        }
    }

    private async void CreateProfile_Click(object sender, RoutedEventArgs e)
    {
        var editor = new ProfileEditorWindow { Owner = this };
        if (editor.ShowDialog() != true) return;
        var profile = editor.CreateProfile();
        await _profiles.AddAsync(profile);
        await LoadProfilesAsync();
        ProfilesList.SelectedItem = Cards.FirstOrDefault(x => x.Profile.Id == profile.Id);
    }

    private void SelectProfile_Click(object sender, RoutedEventArgs e)
    {
        SelectedProfile = (ProfilesList.SelectedItem as ProfileCard)?.Profile;
        if (SelectedProfile is null) return;
        DialogResult = true;
    }

    private async void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        if ((ProfilesList.SelectedItem as ProfileCard)?.Profile is not { } profile) return;
        var editor = new ProfileEditorWindow(profile) { Owner = this };
        if (editor.ShowDialog() != true) return;
        editor.ApplyTo(profile);
        await _profiles.UpdateAsync(profile);
        await LoadProfilesAsync();
        ProfilesList.SelectedItem = Cards.FirstOrDefault(x => x.Profile.Id == profile.Id);
    }
}

public sealed class ProfileCard
{
    private readonly Profile _profile;
    public ProfileCard(Profile profile) => _profile = profile;
    public Profile Profile => _profile;
    public string Name => _profile.Name;
    public string Initial => string.IsNullOrWhiteSpace(_profile.Name) ? "?" : _profile.Name[..1];
    public Brush AvatarBrush => _profile.Avatar switch
    {
        "coral" => new LinearGradientBrush(
            Color.FromRgb(251, 113, 133), // #FB7185
            Color.FromRgb(225, 29, 72),   // #E11D48
            new Point(0, 0), new Point(1, 1)),
        "ocean" => new LinearGradientBrush(
            Color.FromRgb(56, 189, 248),  // #38BDF8
            Color.FromRgb(2, 132, 199),   // #0284C7
            new Point(0, 0), new Point(1, 1)),
        "forest" => new LinearGradientBrush(
            Color.FromRgb(52, 211, 153),  // #34D399
            Color.FromRgb(5, 150, 105),   // #059669
            new Point(0, 0), new Point(1, 1)),
        _ => new LinearGradientBrush(
            Color.FromRgb(167, 139, 250), // #A78BFA
            Color.FromRgb(109, 40, 217),  // #6D28D9
            new Point(0, 0), new Point(1, 1))
    };
    public ImageSource? AvatarImage
    {
        get
        {
            if (!File.Exists(_profile.Avatar)) return null;
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(_profile.Avatar, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}

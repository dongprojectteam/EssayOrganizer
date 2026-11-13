using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EssayOrganizer.App.Models;
using EssayOrganizer.App.Data;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace EssayOrganizer.App.Views;

public partial class ProfileEditorWindow : FluentWindow
{
    private string? _photoPath;

    public ProfileEditorWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdatePreview();
    }

    public ProfileEditorWindow(Profile profile) : this()
    {
        Title = "프로파일 수정";
        HeaderText.Text = "프로파일 수정";
        SaveButton.Content = "저장";
        NameBox.Text = profile.Name;
        SetAvatar(profile.Avatar);
    }

    private void NameBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void ChoosePhoto_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "이미지 파일|*.png;*.jpg;*.jpeg;*.bmp|모든 파일|*.*" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var appPaths = AppPaths.CreateDefault();
            appPaths.EnsureDirectories();

            var ext = Path.GetExtension(dialog.FileName);
            var safeFileName = $"avatar_{Guid.NewGuid():N}{ext}";
            var destPath = Path.Combine(appPaths.ProfileImagesDirectory, safeFileName);

            File.Copy(dialog.FileName, destPath, overwrite: true);
            _photoPath = destPath;
            PhotoText.Text = Path.GetFileName(dialog.FileName) + " (앱에 안전하게 보관됨)";
            RemovePhotoButton.Visibility = Visibility.Visible;
            UpdatePreview();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"사진 복사 중 오류가 발생했습니다: {ex.Message}", "오류", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemovePhoto_Click(object sender, RoutedEventArgs e)
    {
        _photoPath = null;
        PhotoText.Text = "기본 색상 아바타를 사용합니다";
        RemovePhotoButton.Visibility = Visibility.Collapsed;
        UpdatePreview();
    }

    private void DefaultAvatar_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        _photoPath = null;
        PhotoText.Text = "기본 색상 아바타를 사용합니다";
        if (RemovePhotoButton is not null)
            RemovePhotoButton.Visibility = Visibility.Collapsed;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (PreviewInitial is null) return;

        var name = NameBox.Text?.Trim() ?? string.Empty;
        PreviewInitial.Text = string.IsNullOrWhiteSpace(name) ? "?" : name[..1];
        PreviewName.Text = string.IsNullOrWhiteSpace(name) ? "새 프로파일" : name;

        if (!string.IsNullOrEmpty(_photoPath) && File.Exists(_photoPath))
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(_photoPath, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();

                PreviewImage.Source = image;
                PreviewImage.Visibility = Visibility.Visible;
                return;
            }
            catch
            {
                // Fall back to gradient if image fails to load
            }
        }

        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;

        if (Coral.IsChecked == true)
        {
            PreviewAvatarBorder.Background = new LinearGradientBrush(
                Color.FromRgb(251, 113, 133), Color.FromRgb(225, 29, 72), new Point(0, 0), new Point(1, 1));
        }
        else if (Ocean.IsChecked == true)
        {
            PreviewAvatarBorder.Background = new LinearGradientBrush(
                Color.FromRgb(56, 189, 248), Color.FromRgb(2, 132, 199), new Point(0, 0), new Point(1, 1));
        }
        else if (Forest.IsChecked == true)
        {
            PreviewAvatarBorder.Background = new LinearGradientBrush(
                Color.FromRgb(52, 211, 153), Color.FromRgb(5, 150, 105), new Point(0, 0), new Point(1, 1));
        }
        else
        {
            PreviewAvatarBorder.Background = new LinearGradientBrush(
                Color.FromRgb(167, 139, 250), Color.FromRgb(109, 40, 217), new Point(0, 0), new Point(1, 1));
        }
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            System.Windows.MessageBox.Show("프로파일 이름을 입력해 주세요.", "이름 필요", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
            NameBox.Focus();
            return;
        }
        DialogResult = true;
    }

    public Profile CreateProfile() => new()
    {
        Id = Guid.NewGuid(),
        Name = NameBox.Text.Trim(),
        CreatedAt = DateTime.UtcNow,
        Avatar = SelectedAvatar()
    };

    public void ApplyTo(Profile profile)
    {
        profile.Name = NameBox.Text.Trim();
        profile.Avatar = SelectedAvatar();
    }

    private string SelectedAvatar() => _photoPath ?? (Coral.IsChecked == true ? "coral" : Ocean.IsChecked == true ? "ocean" : Forest.IsChecked == true ? "forest" : "violet");

    private void SetAvatar(string avatar)
    {
        if (File.Exists(avatar))
        {
            _photoPath = avatar;
            PhotoText.Text = Path.GetFileName(avatar);
            RemovePhotoButton.Visibility = Visibility.Visible;
            UpdatePreview();
            return;
        }

        RemovePhotoButton.Visibility = Visibility.Collapsed;
        if (avatar == "coral") Coral.IsChecked = true;
        else if (avatar == "ocean") Ocean.IsChecked = true;
        else if (avatar == "forest") Forest.IsChecked = true;
        else Violet.IsChecked = true;
        UpdatePreview();
    }
}

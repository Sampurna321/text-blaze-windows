using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TextBlaze.Models;
using TextBlaze.Services;

namespace TextBlaze;

public partial class MainWindow : Window
{
    private readonly SnippetStore _store = new();
    private readonly TextExpander _expander;
    private AppSettings _settings = new();
    private Snippet? _current;
    private bool _loadingEditor;

    public MainWindow()
    {
        InitializeComponent();
        _settings = AppSettings.Load();
        _store.Load();

        _expander = new TextExpander(_store) { TriggerKey = _settings.TriggerKey };
        RefreshList();
        UpdateSyncUi();
    }

    private void UpdateSyncUi()
    {
        if (_settings.SyncFolder is null)
        {
            SyncStatusText.Text = "  Sync: disabled";
            SyncButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            SyncStatusText.Text = $"  Sync folder: {_settings.SyncFolder}";
            SyncButton.Visibility = Visibility.Visible;
        }
    }

    private void RefreshList()
    {
        var selected = _current?.Id;
        SnippetList.ItemsSource = null;
        SnippetList.ItemsSource = _store.Library.Snippets
            .Where(s => !s.Deleted)
            .OrderBy(s => s.Shortcut, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (selected is not null)
            SnippetList.SelectedItem = SnippetList.ItemsSource
                .Cast<Snippet>().FirstOrDefault(s => s.Id == selected);
    }

    private void NewSnippetButton_Click(object sender, RoutedEventArgs e)
    {
        var snippet = new Snippet { Name = "New snippet" };
        _store.Upsert(snippet);
        RefreshList();
        SnippetList.SelectedItem = SnippetList.ItemsSource.Cast<Snippet>()
            .First(s => s.Id == snippet.Id);
    }

    private void DeleteSnippetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        if (MessageBox.Show($"Delete '{_current.DisplayName}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _store.Delete(_current);
        _current = null;
        EditorPanel.IsEnabled = false;
        RefreshList();
    }

    private void SnippetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _current = SnippetList.SelectedItem as Snippet;
        if (_current is null) return;
        _loadingEditor = true;
        ShortcutBox.Text = _current.Shortcut;
        NameBox.Text = _current.Name;
        BodyBox.Text = _current.Body;
        NotesBox.Text = _current.Notes ?? "";
        RenderImageThumbnails();
        _loadingEditor = false;
        EditorPanel.IsEnabled = true;
    }

    private void Field_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loadingEditor || _current is null) return;
        _current.Shortcut = ShortcutBox.Text;
        _current.Name = NameBox.Text;
        _current.Body = BodyBox.Text;
        _current.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text;
        _store.Upsert(_current);
    }

    private void RenderImageThumbnails()
    {
        ImageList.Children.Clear();
        if (_current is null) return;
        foreach (var img in _current.Images.ToList())
        {
            var path = Path.Combine(AppPaths.ImageCacheDir, img.LocalFile);
            if (!File.Exists(path)) continue;
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(path);
            bmp.EndInit();
            var bmpImage = new BitmapImage(new Uri(path));
            var image = new System.Windows.Shapes.Rectangle { Fill = new ImageBrush(bmpImage) };
            image.Width = 72; image.Height = 72; image.Stretch = Stretch.Uniform;
            image.Margin = new Thickness(0, 0, 6, 6);
            image.Cursor = System.Windows.Input.Cursors.Hand;
            var captured = img;
            image.MouseDown += (_, _) =>
            {
                _current.Images.Remove(captured);
                if (File.Exists(path)) File.Delete(path);
                _store.Upsert(_current);
                RenderImageThumbnails();
            };
            ImageList.Children.Add(image);
            var lbl = new System.Windows.Controls.TextBlock { Text = img.FileName, FontSize = 10, TextWrapping = TextWrapping.Wrap, Width = 72 };
            var stack = new System.Windows.Controls.StackPanel { Children = { image, lbl }, Margin = new Thickness(0, 0, 6, 0) };
            ImageList.Children.Add(stack);
        }
    }

    private void AddImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Choose an image",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All files|*.*",
        };
        if (dlg.ShowDialog(this) != true) return;

        var img = new SnippetImage
        {
            FileName = Path.GetFileName(dlg.FileName),
            LocalFile = $"{Guid.NewGuid():N}{Path.GetExtension(dlg.FileName)}",
            ContentType = ContentTypeFor(dlg.FileName),
        };
        File.Copy(dlg.FileName, Path.Combine(AppPaths.ImageCacheDir, img.LocalFile));
        _current.Images.Add(img);
        _store.Upsert(_current);
        RenderImageThumbnails();
    }

    private static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".webp" => "image/webp",
        _ => "application/octet-stream",
    };

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow(_settings, _expander) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _store.Save();
        }
    }

    private void SyncButton_Click(object sender, RoutedEventArgs e)
    {
        if (_settings.SyncFolder is null) return;
        try
        {
            var syncDir = new DirectoryInfo(_settings.SyncFolder);
            var libDir = Path.Combine(syncDir.FullName, "TextBlaze");
            Directory.CreateDirectory(Path.Combine(libDir, "images"));
            var libFile = Path.Combine(libDir, "library.json");
            File.WriteAllText(libFile, System.Text.Json.JsonSerializer.Serialize(_store.Library));
            foreach (var img in _store.Library.Snippets.SelectMany(s => s.Images)
                .Where(i => File.Exists(Path.Combine(AppPaths.ImageCacheDir, i.LocalFile))))
            {
                var src = Path.Combine(AppPaths.ImageCacheDir, img.LocalFile);
                var dst = Path.Combine(libDir, "images", img.LocalFile);
                File.Copy(src, dst, overwrite: true);
            }
            SetStatus("Synced to folder");
        }
        catch (Exception ex) { SetStatus($"Sync error: {ex.Message}"); }
    }

    private void SetStatus(string msg) => HintText.Text = msg;

    protected override void OnClosed(EventArgs e)
    {
        _settings.TriggerKey = _expander.TriggerKey;
        _settings.Save();
        base.OnClosed(e);
    }
}
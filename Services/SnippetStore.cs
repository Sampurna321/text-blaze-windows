using System.IO;
using System.Text.Json;
using TextBlaze.Models;

namespace TextBlaze.Services;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TextBlaze");

    public static string LibraryFile => Path.Combine(Root, "library.json");
    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string ImageCacheDir => Path.Combine(Root, "images");

    static AppPaths()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(ImageCacheDir);
    }
}

public class SnippetStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public SnippetLibrary Library { get; private set; } = new();

    public event Action? LibraryChanged;

    public void Load()
    {
        if (File.Exists(AppPaths.LibraryFile))
        {
            var json = File.ReadAllText(AppPaths.LibraryFile);
            Library = JsonSerializer.Deserialize<SnippetLibrary>(json, JsonOpts) ?? new SnippetLibrary();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Library, JsonOpts);
        File.WriteAllText(AppPaths.LibraryFile, json);
        LibraryChanged?.Invoke();
    }

    public Snippet? FindByShortcut(string shortcut)
    {
        var norm = shortcut.Trim().ToLowerInvariant();
        return Library.Snippets.FirstOrDefault(s =>
            s.Enabled && !s.Deleted && s.Shortcut.Trim().ToLowerInvariant() == norm);
    }

    public void Upsert(Snippet snippet)
    {
        snippet.UpdatedAtUtc = DateTime.UtcNow;
        var existing = Library.Snippets.FirstOrDefault(s => s.Id == snippet.Id);
        if (existing is null)
        {
            Library.Snippets.Add(snippet);
        }
        else
        {
            var idx = Library.Snippets.IndexOf(existing);
            Library.Snippets[idx] = snippet;
        }
        Save();
    }

    public void Delete(Snippet snippet)
    {
        var existing = Library.Snippets.FirstOrDefault(s => s.Id == snippet.Id);
        if (existing is not null)
        {
            existing.Deleted = true;
            existing.Enabled = false;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            Save();
        }
    }
}

public class AppSettings
{
    public string? SupabaseUrl { get; set; }
    public string? SupabaseAnonKey { get; set; }
    public string? SavedEmail { get; set; }
    public string? SyncFolder { get; set; }
    public bool StartWithWindows { get; set; }
    public string TriggerKey { get; set; } = "Tab";
    public bool AutoSync { get; set; } = true;

    public static AppSettings Load()
    {
        if (File.Exists(AppPaths.SettingsFile))
        {
            var json = File.ReadAllText(AppPaths.SettingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new AppSettings();
        }
        return new AppSettings();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOpts);
        File.WriteAllText(AppPaths.SettingsFile, json);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

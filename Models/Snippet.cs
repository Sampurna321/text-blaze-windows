using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace TextBlaze.Models;

public class Snippet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Shortcut { get; set; } = "";
    public string Name { get; set; } = "";
    public string Body { get; set; } = "";

    // Images referenced by this snippet, in order. Text may embed markers
    // like {image:0} to say where the image goes; otherwise images are
    // appended after the text.
    public List<SnippetImage> Images { get; set; } = new();

    public List<string> Folders { get; set; } = new();
    public string? Notes { get; set; }
    public bool Enabled { get; set; } = true;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; }

    [JsonIgnore]
    public string DisplayName => string.IsNullOrEmpty(Name) ? Shortcut : Name;
}

public class SnippetImage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Remote path in Supabase storage, e.g. "images/<userId>/<id>.png".
    // Null until first synced upload.
    public string? StoragePath { get; set; }

    // Local cache file name inside the app data folder.
    public string LocalFile { get; set; } = "";

    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "image/png";
}

public class SnippetLibrary
{
    public List<Snippet> Snippets { get; set; } = new();
    public List<string> Folders { get; set; } = new();
    public DateTime LastSyncedUtc { get; set; } = DateTime.MinValue;
}

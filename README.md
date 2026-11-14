# Text Blaze for Windows

A native Windows text expander app — type a shortcut (like `;sig`) anywhere and press Tab to expand it to your saved text. Supports:

- **Text snippets** with shortcuts
- **Placeholders**: `{date}`, `{time}`, `{clipboard}`, `{cursor}`, `{form:label}`, `{choice:a|b|c}`
- **Image snippets**: attach images that paste with the text
- **Sync folder**: optional folder-based sync via OneDrive/Dropbox/whatever
- **Start with Windows**: optional auto-start

## Build (requires .NET 8 SDK)

```bash
dotnet build -c Release
```

## Run

```bash
bin/Release/net8.0-windows/TextBlaze.exe
```

## How snippets work

1. Create a snippet by clicking "+ New"
2. Set a shortcut (e.g., `;sig`) and body text
3. Use placeholders: `{date}` → today's date, `{clipboard}` → current clipboard, etc.
4. Add images to a snippet — they paste after the text when expanded
5. Set a sync folder in Settings to sync snippets via your cloud storage

## Development

This is built with WPF (.NET 8) and uses:
- `System.Windows.Forms` integration for the keyboard hook
- JSON files for local persistence
- Simple path-based sync (copy library.json to sync folder)

## License

MIT — feel free to modify and use.
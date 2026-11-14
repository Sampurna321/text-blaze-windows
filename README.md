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

**Recommended: Double-click from File Explorer**
1. Navigate to `bin\Release\net8.0-windows\`
2. Double-click `TextBlaze.exe`

**Or from PowerShell:**
```powershell
cd "E:\Reverse engineer\TextBlaze\bin\Release\net8.0-windows"
.\TextBlaze.exe
```

**Note:** Avoid running from Git Bash/MSYS2 since it interferes with the WPF window display.

## How snippets work

1. Create a snippet by clicking "+ New"
2. Set a shortcut (e.g., `;sig`) and body text with placeholders like `{date}`:2025-04-05
3. Use placeholders: `{date}` → today's date, `{clipboard}` → current clipboard, etc.
4. Add images to a snippet — they paste after the text when expanded
5. Set a sync folder in Settings to sync snippets via your cloud storage

## Exit/Cleanup

To close the app:
- Click the X on the window, or
- Right-click the Tray icon (coming soon) and select "Exit"
- Or: `taskkill /IM TextBlaze.exe`

## Development

This is built with WPF (.NET 8) and uses:
- Low-level keyboard hook via Win32 API
- JSON files for local persistence
- Simple path-based sync (copy library.json to sync folder)

## License

MIT — feel free to modify and use.
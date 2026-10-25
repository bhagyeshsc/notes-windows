# Notes

### A little room to think.

Notes is a small, offline-first notes app for Windows. Keep your ideas in a simple list, open each one in its own window, and arrange your thoughts around whatever you're working on. Saved notes write to your computer as you type. Temporary notes give passing thoughts somewhere to land.

**Free to use. Open source. No account, subscription, or cloud service required.**

[Download for Windows](https://github.com/bhagyeshsc/notes-windows/releases/latest) · [Report an issue](https://github.com/bhagyeshsc/notes-windows/issues)

<img width="1122" height="1080" alt="image" src="https://github.com/user-attachments/assets/c0faad35-2617-4284-a7bf-4ee76b990b59" />


## Small app. Thoughtful details.

- **Independent floating notes.** Move, resize, pin, and close each note separately.
- **Save as you type.** Every title, body, and link change in a saved note is written locally.
- **Temporary scratchpads.** Use a Temp note for quick working thoughts. Save it when it becomes something worth keeping.
- **One tidy collection.** Open a saved note from the list; select it again to bring its window forward.
- **Links, your way.** Add a URL with an optional title and display text.
- **Light, dark, or Windows.** Choose a theme, adjust the text size, and enjoy gentle motion that follows Windows' animation preference.
- **Quiet when you need it.** Hide the collection to the system tray while your notes stay open. Startup and restoring saved note windows are optional.

## Download and run

Open [Releases](https://github.com/bhagyeshsc/notes-windows/releases/latest) and download `Notes-Setup.exe` to install, or `Notes.exe` to run standalone. The optional Windows ZIP contains both; extract it before running an app. Keep a copy of the included MIT licence when redistributing.

| File | Use it for |
| --- | --- |
| `Notes-Setup.exe` | Install for your Windows account, with Start menu entries and an uninstaller. Administrator access is not required. |
| `Notes.exe` | Run the portable app without installing. |
| `LICENSE` | The MIT licence covering the app and source. |

**Requirements:** 64-bit Windows 10 or Windows 11, with the Windows .NET Framework WPF and Windows Forms components enabled. The app is currently unsigned, so Windows may show an unrecognized publisher notice. Downloads are not a promise of bug-free operation; please report issues and keep backups of important notes.

To update, choose **Exit Notes** from the menu or **Exit** from the tray before replacing the app. Your saved notes live separately from the program files.

## Your first note

1. Choose **+ Note** for something to keep, or **+ Temp note** for a scratchpad.
2. Write a title and start typing. Saved notes save every change automatically.
3. Drag a note by its header, resize from its edges, or pin it above other windows.
4. Close a saved note to put it away. It stays in your collection.

**Temporary notes disappear when closed.** Choose **Save note** from the note's three-dot menu before closing one you want to keep. Temporary notes are kept in memory until saved and do not survive exiting the app.

Closing the main list hides it to the tray and keeps other note windows open. Closing a note affects only that note. **Exit Notes** closes the whole app.

### Add a link

Press **Ctrl+K** inside a note, or choose **Add link** from its menu. A web URL is required; title and link text are optional. Display text falls back to the title, then the URL. Titles are entered manually.

Click a link to open it in your default browser. Right-click it, or use **Shift+F10**, to edit, copy, or remove it. Use **Ctrl+click** to open a plain web URL in the note body.

## Your notes stay on your computer

Notes has no account system, cloud sync, analytics, or automatic webpage fetching. Opening a web link launches your browser and connects to that website.

Saved data is stored as plain-text JSON in:

```text
%LOCALAPPDATA%\Notes\
  notes.json
  settings.json
```

The app uses temporary files and backup copies when replacing saved data. This is local storage, not encrypted storage or a full backup system. To make a backup, exit Notes and copy the entire folder somewhere safe. Uninstalling keeps this data folder.

## Build from source

No third-party packages or package restore are needed. The app is written in C# with WPF; the tray and installer use Windows Forms.

On 64-bit Windows, open PowerShell in the repository and run:

```powershell
.\build.ps1
```

The script uses the .NET Framework C# compiler at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` and the matching WPF assemblies. It produces the app, installer, licence, README, and a source archive in `dist/`. You can choose a different destination:

```powershell
.\build.ps1 -OutputDirectory C:\Builds\Notes
```

For development, keep test notes separate from real notes:

```powershell
.\dist\Notes.exe --data-dir=C:\Temp\Notes-Test
```

| Source | Responsibility |
| --- | --- |
| `Notes.cs` | Notes, persistence, windows, preferences, and tray behavior |
| `Design.cs` | Icons, theme controls, and motion |
| `Links.cs` | Link editing and web URL validation |
| `Scrollbars.cs` | Themed scrollbars |
| `Setup.cs` | Per-user installation and uninstallation |
| `BuildAssets.cs` | App icon generation |

## Contributing

Bug reports, accessibility improvements, and focused pull requests are welcome. When reporting a problem, include your Windows version, what you did, what you expected, and what happened. Remove private note content from screenshots and examples.

Keep changes small and preserve the app's focus: quick writing, independent windows, and dependable local saving. Before proposing a change, build it and check the affected flow in both themes. Use an isolated data folder when testing saving or deleting notes.

## Licence

Notes is released under the [MIT licence](LICENSE). You may use, copy, modify, and redistribute it, including commercially, provided the copyright and licence notice are preserved. The software is provided as-is, without warranty.

Made with ❤️ by bhagyesh  
Infinite Licence for you.

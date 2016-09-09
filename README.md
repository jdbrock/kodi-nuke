# kodi-nuke
Quick and dirty Windows app to synchronise deleting TV shows across Kodi &amp; Sonarr.

Files will be permanently deleted from disk by Sonarr - be careful.

## Usage

- Enable the Kodi web interface in `System → Settings → Services → Web server`.
- Modify the following two lines in `MainWindow.xaml.cs`:
```csharp
_sClient = new SonarrClient("http://YOUR-SONARR-IP:8989/api/", "YOUR-SONARR-API-KEY");
_kClient = new KodiClient("YOUR-KODI-IP", userName: "KODI-USERNAME", password: "KODI-PASSWORD");
```

- Run the app.
- Pick a series.
- Hit delete.

## Notes

- TV shows which can't be matched (by their TVDB ID) between Sonarr and Kodi will not be shown.

## Screenshots

![screenshot](https://i.imgur.com/4Lr9u0b.png "Screenshot")

# Reise- und Arbeitszeitrechner

Windows-Anwendung zur Berechnung und Verwaltung von Reisezeiten, Zeitzonen,
Arbeitszeiten und Überstunden.

## Entwicklung

Voraussetzungen:

- Windows
- .NET 8 SDK
- Inno Setup 6 für den Installer

```powershell
dotnet build .\ReiseArbeitszeitApp.csproj
.\BuildInstaller.ps1
```

Die lokale Datenbank und die Einstellungen werden unter
`%LOCALAPPDATA%\ReiseArbeitszeitApp` gespeichert und bei Updates nicht ersetzt.

## Version veröffentlichen

1. Version in `ReiseArbeitszeitApp.csproj` erhöhen.
2. Änderungen committen.
3. Git-Tag im Format `v0.1.1` erstellen und zu GitHub übertragen.
4. GitHub Actions erstellt und veröffentlicht EXE, ZIP und Installer.


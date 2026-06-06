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

Vor einer notwendigen Datenbankmigration erstellt die App automatisch eine
Sicherung unter `%LOCALAPPDATA%\ReiseArbeitszeitApp\Backups`. Jede Migration
läuft in einer Transaktion und wird in der Tabelle `SchemaMigrations`
protokolliert.

## Version veröffentlichen

1. Version in `ReiseArbeitszeitApp.csproj` erhöhen.
2. Änderungen committen.
3. Git-Tag passend zur Projektversion erstellen, zum Beispiel `v0.1.3`, und zu GitHub übertragen.
4. GitHub Actions erstellt und veröffentlicht EXE, ZIP und Installer.

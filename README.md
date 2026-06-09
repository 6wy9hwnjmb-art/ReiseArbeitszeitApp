# Reise- und Arbeitszeitrechner

Windows-Anwendung zur Berechnung und Verwaltung von Reisezeiten, Zeitzonen,
Arbeitszeiten und Überstunden.

## Funktionen

- Reisezeitberechnung mit automatischer Zeitzonenerkennung
- Arbeitszeiterfassung mit Ländern, Einsatzorten, Tagesarten und Feiertagen für deutsche Bundesländer und Schweizer Kantone
- Eigene Feiertage, lokale Ausnahmen und deaktivierbare automatische Feiertage
- Monatskalender und Jahresauswertung
- Monats- und Jahresvergleich der Arbeitszeit nach Land und Einsatzort
- Mehrtageserfassung für Urlaub, Krankheit, Homeoffice und Feiertage
- Manuelle und tägliche automatische Datensicherungen
- Geprüfte Wiederherstellung mit zusätzlicher Rettungskopie
- Integrierte Updateprüfung und Windows-Installer

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

Manuelle und automatische Sicherungen werden ebenfalls in diesem Ordner
verwaltet. Vor jeder Wiederherstellung legt die App eine Rettungskopie des
aktuellen Datenstands an und prüft die ausgewählte SQLite-Datei.

## Version veröffentlichen

1. Version in `ReiseArbeitszeitApp.csproj` erhöhen.
2. Änderungen committen.
3. Git-Tag passend zur Projektversion erstellen, zum Beispiel `v0.2.1`, und zu GitHub übertragen.
4. GitHub Actions erstellt und veröffentlicht EXE, ZIP und Installer.

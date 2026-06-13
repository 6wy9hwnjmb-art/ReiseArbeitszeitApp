# Reise- und Arbeitszeitrechner

Windows-Anwendung zur Berechnung und Verwaltung von Reisezeiten, Zeitzonen,
Arbeitszeiten und Überstunden.

## Funktionen

- Reisezeitberechnung mit automatischer Zeitzonenerkennung
- Arbeitszeiterfassung mit Ländern, Einsatzorten, Tagesarten und Feiertagen für deutsche Bundesländer und Schweizer Kantone
- Dynamische Arbeitszeiterfassung mit automatisch berechnetem Arbeitsende
- Editierbare Auswahl bereits verwendeter Arbeitsorte nach Land
- Eigene Feiertage, lokale Ausnahmen und deaktivierbare automatische Feiertage
- Monatskalender und Jahresauswertung
- Monats- und Jahresvergleich der Arbeitszeit nach Land und Einsatzort
- CSV-Export in einen frei wählbaren Ausgabeordner
- Mehrtageserfassung für Urlaub, Krankheit, Homeoffice und Feiertage
- Manuelle und tägliche automatische Datensicherungen
- Geprüfte Wiederherstellung mit zusätzlicher Rettungskopie
- Integrierte Updateprüfung und Windows-Installer
- Plattformunabhängige Core-Bibliothek und erster Sync-API-Prototyp als Grundlage für iOS

## Entwicklung

Voraussetzungen:

- Windows
- .NET 8 SDK
- Inno Setup 6 für den Installer

```powershell
dotnet build .\ReiseArbeitszeitApp.csproj
dotnet build .\ReiseArbeitszeitApp.SyncApi\ReiseArbeitszeitApp.SyncApi.csproj
.\BuildInstaller.ps1
```

## iOS- und Sync-Grundlage

Die plattformunabhängigen Modelle und Berechnungen liegen in
`ReiseArbeitszeitApp.Core`. Dieses Projekt kann später direkt von einer
.NET-MAUI-iOS-App verwendet werden.

`ReiseArbeitszeitApp.SyncApi` enthält einen ersten lokalen Push/Pull-Prototyp:

```powershell
dotnet run --project .\ReiseArbeitszeitApp.SyncApi\ReiseArbeitszeitApp.SyncApi.csproj
```

Die Endpunkte sind `/health`, `/api/sync/push` und `/api/sync/pull`. Der
aktuelle Speicher ist nur für Entwicklung und Tests gedacht. Vor einem
öffentlichen Betrieb folgen Benutzeranmeldung, verschlüsselte Cloud-Datenbank
und dauerhafte Konfliktauflösung.

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
3. Git-Tag passend zur Projektversion erstellen, zum Beispiel `vX.Y.Z`, und zu GitHub übertragen.
4. GitHub Actions erstellt und veröffentlicht EXE, ZIP und Installer.

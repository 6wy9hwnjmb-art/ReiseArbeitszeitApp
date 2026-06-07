using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ReiseArbeitszeitApp.Services;

public enum DatabaseBackupKind
{
    Manual,
    Automatic,
    BeforeRestore,
    Migration
}

public sealed class DatabaseBackupInfo
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required long SizeBytes { get; init; }
    public required DatabaseBackupKind Kind { get; init; }

    public string CreatedText => CreatedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("de-DE"));
    public string SizeText => SizeBytes < 1024 * 1024
        ? $"{Math.Max(1, SizeBytes / 1024.0):0} KB"
        : $"{SizeBytes / 1024.0 / 1024.0:0.0} MB";
    public string KindText => Kind switch
    {
        DatabaseBackupKind.Automatic => "Automatisch",
        DatabaseBackupKind.BeforeRestore => "Vor Wiederherstellung",
        DatabaseBackupKind.Migration => "Datenbankmigration",
        _ => "Manuell"
    };
}

public sealed class DatabaseBackupService
{
    private const int MaximumAutomaticBackups = 20;
    private readonly string _backupFolder;

    public DatabaseBackupService(string? backupFolder = null)
    {
        _backupFolder = backupFolder
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ReiseArbeitszeitApp",
                "Backups");
        Directory.CreateDirectory(_backupFolder);
    }

    public string BackupFolder => _backupFolder;

    public DatabaseBackupInfo CreateManualBackup(string databasePath)
    {
        return CreateBackup(databasePath, DatabaseBackupKind.Manual);
    }

    public DatabaseBackupInfo? CreateAutomaticBackupIfDue(
        string databasePath,
        TimeSpan minimumInterval)
    {
        var latestAutomatic = GetBackups()
            .Where(backup => backup.Kind == DatabaseBackupKind.Automatic)
            .MaxBy(backup => backup.CreatedAt);

        if (latestAutomatic is not null
            && DateTime.Now - latestAutomatic.CreatedAt < minimumInterval)
        {
            return null;
        }

        var backup = CreateBackup(databasePath, DatabaseBackupKind.Automatic);
        RemoveOldAutomaticBackups();
        return backup;
    }

    public IReadOnlyList<DatabaseBackupInfo> GetBackups()
    {
        if (!Directory.Exists(_backupFolder))
            return [];

        return Directory.EnumerateFiles(_backupFolder, "reise_arbeitszeit_*.db")
            .Select(path =>
            {
                var file = new FileInfo(path);
                return new DatabaseBackupInfo
                {
                    FilePath = file.FullName,
                    FileName = file.Name,
                    CreatedAt = file.LastWriteTime,
                    SizeBytes = file.Length,
                    Kind = GetKind(file.Name)
                };
            })
            .OrderByDescending(backup => backup.CreatedAt)
            .ToList();
    }

    public string RestoreDatabase(string databasePath, string backupPath)
    {
        ValidateBackup(backupPath);
        var safetyBackup = CreateBackup(databasePath, DatabaseBackupKind.BeforeRestore);
        var databaseFolder = Path.GetDirectoryName(Path.GetFullPath(databasePath))
            ?? throw new InvalidOperationException("Der Datenbankordner konnte nicht ermittelt werden.");
        var temporaryPath = Path.Combine(databaseFolder, $".restore-{Guid.NewGuid():N}.db");

        try
        {
            File.Copy(backupPath, temporaryPath, overwrite: false);
            ValidateBackup(temporaryPath);
            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
                File.Replace(temporaryPath, databasePath, null);
            else
                File.Move(temporaryPath, databasePath);

            SqliteConnection.ClearAllPools();
            return safetyBackup.FilePath;
        }
        catch
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            throw;
        }
    }

    public void DeleteBackup(string backupPath)
    {
        var fullPath = Path.GetFullPath(backupPath);
        var backupRoot = Path.GetFullPath(_backupFolder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(backupRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Die ausgewählte Datei liegt nicht im Sicherungsordner.");

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public void ValidateBackup(string backupPath)
    {
        if (!File.Exists(backupPath))
            throw new InvalidOperationException("Die Sicherungsdatei wurde nicht gefunden.");

        try
        {
            using var connection = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = backupPath,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString());
            connection.Open();

            using var integrityCommand = connection.CreateCommand();
            integrityCommand.CommandText = "PRAGMA integrity_check;";
            var integrityResult = Convert.ToString(integrityCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"SQLite-Integritätsprüfung: {integrityResult}");

            foreach (var tableName in new[] { "WorkDays", "Trips" })
            {
                using var tableCommand = connection.CreateCommand();
                tableCommand.CommandText = """
                    SELECT COUNT(*)
                    FROM sqlite_master
                    WHERE type = 'table' AND name = $TableName;
                    """;
                tableCommand.Parameters.AddWithValue("$TableName", tableName);
                if (Convert.ToInt32((long)tableCommand.ExecuteScalar()!) == 0)
                    throw new InvalidOperationException($"Die Tabelle '{tableName}' fehlt.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Die ausgewählte Datei ist keine gültige Sicherung dieser Anwendung.",
                ex);
        }
    }

    private DatabaseBackupInfo CreateBackup(string databasePath, DatabaseBackupKind kind)
    {
        if (!File.Exists(databasePath))
            throw new InvalidOperationException("Die Datenbankdatei wurde nicht gefunden.");

        Directory.CreateDirectory(_backupFolder);
        var kindName = kind switch
        {
            DatabaseBackupKind.Automatic => "automatisch",
            DatabaseBackupKind.BeforeRestore => "vor-wiederherstellung",
            _ => "manuell"
        };
        var backupPath = Path.Combine(
            _backupFolder,
            $"reise_arbeitszeit_{kindName}_{DateTime.Now:yyyyMMdd_HHmmssfff}.db");

        try
        {
            using var source = new SqliteConnection($"Data Source={databasePath}");
            using var destination = new SqliteConnection($"Data Source={backupPath}");
            source.Open();
            destination.Open();
            source.BackupDatabase(destination);
            ValidateBackup(backupPath);

            var file = new FileInfo(backupPath);
            return new DatabaseBackupInfo
            {
                FilePath = file.FullName,
                FileName = file.Name,
                CreatedAt = file.LastWriteTime,
                SizeBytes = file.Length,
                Kind = kind
            };
        }
        catch
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(backupPath))
                File.Delete(backupPath);
            throw;
        }
    }

    private void RemoveOldAutomaticBackups()
    {
        var oldBackups = GetBackups()
            .Where(backup => backup.Kind == DatabaseBackupKind.Automatic)
            .Skip(MaximumAutomaticBackups)
            .ToList();

        foreach (var backup in oldBackups)
            DeleteBackup(backup.FilePath);
    }

    private static DatabaseBackupKind GetKind(string fileName)
    {
        if (fileName.Contains("_automatisch_", StringComparison.OrdinalIgnoreCase))
            return DatabaseBackupKind.Automatic;
        if (fileName.Contains("_vor-wiederherstellung_", StringComparison.OrdinalIgnoreCase))
            return DatabaseBackupKind.BeforeRestore;
        if (fileName.Contains("_schema-", StringComparison.OrdinalIgnoreCase))
            return DatabaseBackupKind.Migration;
        return DatabaseBackupKind.Manual;
    }
}

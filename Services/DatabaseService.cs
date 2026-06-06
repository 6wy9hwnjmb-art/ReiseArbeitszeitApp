using System;
using System.IO;
using Microsoft.Data.Sqlite;
using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Services;


public class DatabaseService
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public DatabaseService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReiseArbeitszeitApp");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "reise_arbeitszeit.db");
        _connectionString = $"Data Source={_dbPath}";
        Initialize();
    }

    public string DatabasePath => _dbPath;

    private void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        ExecuteNonQuery(connection, """
            CREATE TABLE IF NOT EXISTS WorkDays (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Date TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                BreakTime TEXT NOT NULL,
                TargetTime TEXT NOT NULL,
                Location TEXT NOT NULL,
                Note TEXT NOT NULL,
                IsTravelDay INTEGER NOT NULL,
                TravelWorkTime TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, """
            CREATE TABLE IF NOT EXISTS Trips (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DepartureLocation TEXT NOT NULL,
                ArrivalLocation TEXT NOT NULL,
                DepartureLocalDateTime TEXT NOT NULL,
                ArrivalLocalDateTime TEXT NOT NULL,
                DepartureTimeZoneId TEXT NOT NULL,
                ArrivalTimeZoneId TEXT NOT NULL,
                TravelTime TEXT NOT NULL,
                TimeDifference TEXT NOT NULL,
                Note TEXT NOT NULL
            );
            """);
    }

    public void SaveWorkDay(WorkDay day)
    {
        if (day.Id > 0)
        {
            UpdateWorkDay(day);
            return;
        }

        InsertWorkDay(day);
    }

    private void InsertWorkDay(WorkDay day)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO WorkDays (Date, StartTime, EndTime, BreakTime, TargetTime, Location, Note, IsTravelDay, TravelWorkTime)
            VALUES ($Date, $StartTime, $EndTime, $BreakTime, $TargetTime, $Location, $Note, $IsTravelDay, $TravelWorkTime);
            """;
        command.Parameters.AddWithValue("$Date", day.Date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$StartTime", day.StartTime.ToString());
        command.Parameters.AddWithValue("$EndTime", day.EndTime.ToString());
        command.Parameters.AddWithValue("$BreakTime", day.BreakTime.ToString());
        command.Parameters.AddWithValue("$TargetTime", day.TargetTime.ToString());
        command.Parameters.AddWithValue("$Location", day.Location);
        command.Parameters.AddWithValue("$Note", day.Note);
        command.Parameters.AddWithValue("$IsTravelDay", day.IsTravelDay ? 1 : 0);
        command.Parameters.AddWithValue("$TravelWorkTime", day.TravelWorkTime.ToString());
        command.ExecuteNonQuery();
        command.CommandText = "SELECT last_insert_rowid();";
        command.Parameters.Clear();
        day.Id = Convert.ToInt32((long)command.ExecuteScalar()!);
    }

    private void UpdateWorkDay(WorkDay day)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE WorkDays
            SET Date = $Date,
                StartTime = $StartTime,
                EndTime = $EndTime,
                BreakTime = $BreakTime,
                TargetTime = $TargetTime,
                Location = $Location,
                Note = $Note,
                IsTravelDay = $IsTravelDay,
                TravelWorkTime = $TravelWorkTime
            WHERE Id = $Id;
            """;
        command.Parameters.AddWithValue("$Id", day.Id);
        command.Parameters.AddWithValue("$Date", day.Date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$StartTime", day.StartTime.ToString());
        command.Parameters.AddWithValue("$EndTime", day.EndTime.ToString());
        command.Parameters.AddWithValue("$BreakTime", day.BreakTime.ToString());
        command.Parameters.AddWithValue("$TargetTime", day.TargetTime.ToString());
        command.Parameters.AddWithValue("$Location", day.Location);
        command.Parameters.AddWithValue("$Note", day.Note);
        command.Parameters.AddWithValue("$IsTravelDay", day.IsTravelDay ? 1 : 0);
        command.Parameters.AddWithValue("$TravelWorkTime", day.TravelWorkTime.ToString());
        command.ExecuteNonQuery();
    }

    public void SaveTrip(TripEntry trip)
    {
        if (trip.Id > 0)
        {
            UpdateTrip(trip);
            return;
        }

        InsertTrip(trip);
    }

    private void InsertTrip(TripEntry trip)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Trips (DepartureLocation, ArrivalLocation, DepartureLocalDateTime, ArrivalLocalDateTime, DepartureTimeZoneId, ArrivalTimeZoneId, TravelTime, TimeDifference, Note)
            VALUES ($DepartureLocation, $ArrivalLocation, $DepartureLocalDateTime, $ArrivalLocalDateTime, $DepartureTimeZoneId, $ArrivalTimeZoneId, $TravelTime, $TimeDifference, $Note);
            """;
        command.Parameters.AddWithValue("$DepartureLocation", trip.DepartureLocation);
        command.Parameters.AddWithValue("$ArrivalLocation", trip.ArrivalLocation);
        command.Parameters.AddWithValue("$DepartureLocalDateTime", trip.DepartureLocalDateTime.ToString("O"));
        command.Parameters.AddWithValue("$ArrivalLocalDateTime", trip.ArrivalLocalDateTime.ToString("O"));
        command.Parameters.AddWithValue("$DepartureTimeZoneId", trip.DepartureTimeZoneId);
        command.Parameters.AddWithValue("$ArrivalTimeZoneId", trip.ArrivalTimeZoneId);
        command.Parameters.AddWithValue("$TravelTime", trip.TravelTime.ToString());
        command.Parameters.AddWithValue("$TimeDifference", trip.TimeDifference.ToString());
        command.Parameters.AddWithValue("$Note", trip.Note);
        command.ExecuteNonQuery();
        command.CommandText = "SELECT last_insert_rowid();";
        command.Parameters.Clear();
        trip.Id = Convert.ToInt32((long)command.ExecuteScalar()!);
    }

    private void UpdateTrip(TripEntry trip)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Trips
            SET DepartureLocation = $DepartureLocation,
                ArrivalLocation = $ArrivalLocation,
                DepartureLocalDateTime = $DepartureLocalDateTime,
                ArrivalLocalDateTime = $ArrivalLocalDateTime,
                DepartureTimeZoneId = $DepartureTimeZoneId,
                ArrivalTimeZoneId = $ArrivalTimeZoneId,
                TravelTime = $TravelTime,
                TimeDifference = $TimeDifference,
                Note = $Note
            WHERE Id = $Id;
            """;
        command.Parameters.AddWithValue("$Id", trip.Id);
        command.Parameters.AddWithValue("$DepartureLocation", trip.DepartureLocation);
        command.Parameters.AddWithValue("$ArrivalLocation", trip.ArrivalLocation);
        command.Parameters.AddWithValue("$DepartureLocalDateTime", trip.DepartureLocalDateTime.ToString("O"));
        command.Parameters.AddWithValue("$ArrivalLocalDateTime", trip.ArrivalLocalDateTime.ToString("O"));
        command.Parameters.AddWithValue("$DepartureTimeZoneId", trip.DepartureTimeZoneId);
        command.Parameters.AddWithValue("$ArrivalTimeZoneId", trip.ArrivalTimeZoneId);
        command.Parameters.AddWithValue("$TravelTime", trip.TravelTime.ToString());
        command.Parameters.AddWithValue("$TimeDifference", trip.TimeDifference.ToString());
        command.Parameters.AddWithValue("$Note", trip.Note);
        command.ExecuteNonQuery();
    }

    public void DeleteWorkDay(int id)
    {
        DeleteById("WorkDays", id);
    }

    public void DeleteTrip(int id)
    {
        DeleteById("Trips", id);
    }

    public bool HasWorkDayOnDate(DateTime date, int excludingId = 0)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM WorkDays
            WHERE Date = $Date
              AND Id <> $ExcludingId;
            """;
        command.Parameters.AddWithValue("$Date", date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$ExcludingId", excludingId);

        return Convert.ToInt32((long)command.ExecuteScalar()!) > 0;
    }

    public List<WorkDay> GetWorkDays(int? year = null)
    {
        var result = new List<WorkDay>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = year.HasValue
            ? "SELECT * FROM WorkDays WHERE substr(Date, 1, 4) = $Year ORDER BY Date DESC, Id DESC"
            : "SELECT * FROM WorkDays ORDER BY Date DESC, Id DESC";
        if (year.HasValue)
            command.Parameters.AddWithValue("$Year", year.Value.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new WorkDay
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Date = DateTime.Parse(reader.GetString(reader.GetOrdinal("Date"))),
                StartTime = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("StartTime"))),
                EndTime = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("EndTime"))),
                BreakTime = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("BreakTime"))),
                TargetTime = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("TargetTime"))),
                Location = reader.GetString(reader.GetOrdinal("Location")),
                Note = reader.GetString(reader.GetOrdinal("Note")),
                IsTravelDay = reader.GetInt32(reader.GetOrdinal("IsTravelDay")) == 1,
                TravelWorkTime = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("TravelWorkTime")))
            });
        }
        return result;
    }

    public List<TripEntry> GetTrips(int? year = null)
    {
        var result = new List<TripEntry>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = year.HasValue
            ? "SELECT * FROM Trips WHERE substr(DepartureLocalDateTime, 1, 4) = $Year ORDER BY DepartureLocalDateTime DESC, Id DESC"
            : "SELECT * FROM Trips ORDER BY DepartureLocalDateTime DESC, Id DESC";
        if (year.HasValue)
            command.Parameters.AddWithValue("$Year", year.Value.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new TripEntry
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                DepartureLocation = reader.GetString(reader.GetOrdinal("DepartureLocation")),
                ArrivalLocation = reader.GetString(reader.GetOrdinal("ArrivalLocation")),
                DepartureLocalDateTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("DepartureLocalDateTime"))),
                ArrivalLocalDateTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("ArrivalLocalDateTime"))),
                DepartureTimeZoneId = reader.GetString(reader.GetOrdinal("DepartureTimeZoneId")),
                ArrivalTimeZoneId = reader.GetString(reader.GetOrdinal("ArrivalTimeZoneId")),
                TravelTime = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("TravelTime"))),
                TimeDifference = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("TimeDifference"))),
                Note = reader.GetString(reader.GetOrdinal("Note"))
            });
        }
        return result;
    }

    public YearReport GetYearReport(int year)
    {
        var days = GetWorkDays(year);
        return new YearReport
        {
            Year = year,
            WorkDayCount = days.Count,
            TravelDayCount = days.Count(x => x.IsTravelDay),
            TotalActualWorkTime = days.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.ActualWorkTime),
            TotalTargetWorkTime = days.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.TargetTime),
            TotalTravelWorkTime = days.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.TravelWorkTime)
        };
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private void DeleteById(string tableName, int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {tableName} WHERE Id = $Id;";
        command.Parameters.AddWithValue("$Id", id);
        command.ExecuteNonQuery();
    }
}

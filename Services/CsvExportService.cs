using System.Globalization;
using System.IO;
using System.Text;
using ReiseArbeitszeitApp.Helpers;
using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Services;

public class CsvExportService
{
    public string ExportWorkDays(int year, IReadOnlyCollection<WorkDay> days, string? outputFolder = null)
    {
        var folder = ResolveOutputFolder(outputFolder);
        var path = Path.Combine(folder, $"Arbeitszeit_{year}.csv");
        WriteWorkDays(path, days);
        return path;
    }

    public string ExportWorkDays(int year, int month, IReadOnlyCollection<WorkDay> days, string? outputFolder = null)
    {
        var folder = ResolveOutputFolder(outputFolder);
        var path = Path.Combine(folder, $"Arbeitszeit_{year}_{month:00}.csv");
        WriteWorkDays(path, days);
        return path;
    }

    private static void WriteWorkDays(string path, IReadOnlyCollection<WorkDay> days)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Datum;Tagesart;Beginn;Ende;Pause;Sollzeit;Reisezeit angerechnet;Istzeit;Überstunden;Reisetag;Ort;Notiz");

        foreach (var day in days.OrderBy(x => x.Date))
        {
            sb.AppendLine(string.Join(';', new[]
            {
                day.Date.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE")),
                day.DayTypeText,
                TimeFormatter.FormatClock(day.StartTime),
                TimeFormatter.FormatClock(day.EndTime),
                TimeFormatter.FormatDuration(day.BreakTime),
                TimeFormatter.FormatDuration(day.TargetTime),
                TimeFormatter.FormatDuration(day.TravelWorkTime),
                TimeFormatter.FormatDuration(day.ActualWorkTime),
                TimeFormatter.FormatSignedDuration(day.Overtime),
                day.IsTravelDay ? "Ja" : "Nein",
                Escape(day.Location),
                Escape(day.Note)
            }));
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    private static string ResolveOutputFolder(string? outputFolder)
    {
        var folder = string.IsNullOrWhiteSpace(outputFolder)
            ? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            : outputFolder.Trim();

        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string Escape(string value)
    {
        value = value.Replace("\r", " ").Replace("\n", " ");
        return value.Contains(';') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}

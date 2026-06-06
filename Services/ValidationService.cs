using ReiseArbeitszeitApp.Helpers;
using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Services;

public class ValidationService
{
    public List<string> GetTripWarnings(TripEntry trip)
    {
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(trip.DepartureLocation))
            warnings.Add("Der Startort ist leer.");
        if (string.IsNullOrWhiteSpace(trip.ArrivalLocation))
            warnings.Add("Der Zielort ist leer.");
        if (!string.IsNullOrWhiteSpace(trip.DepartureLocation)
            && string.Equals(trip.DepartureLocation.Trim(), trip.ArrivalLocation.Trim(), StringComparison.OrdinalIgnoreCase))
            warnings.Add("Startort und Zielort sind identisch.");
        if (trip.TravelTime > TimeSpan.FromHours(36))
            warnings.Add($"Die Reisezeit ist sehr lang: {TimeFormatter.Format(trip.TravelTime)}.");
        if (trip.TravelTime < TimeSpan.FromMinutes(30))
            warnings.Add($"Die Reisezeit ist sehr kurz: {TimeFormatter.Format(trip.TravelTime)}.");

        return warnings;
    }

    public List<string> GetWorkDayWarnings(WorkDay day, bool hasDuplicateDate)
    {
        var warnings = new List<string>();

        if (hasDuplicateDate)
            warnings.Add($"Für den {day.Date:dd.MM.yyyy} existiert bereits ein Arbeitstag.");
        if (string.IsNullOrWhiteSpace(day.Location))
            warnings.Add("Ort / Einsatzort ist leer.");
        if (day.EndTime < day.StartTime)
            warnings.Add("Das Arbeitsende liegt vor dem Beginn. Das wird als Nachtschicht über Mitternacht gerechnet.");
        if (day.BreakTime > day.GrossTime)
            warnings.Add("Die Pause ist größer als die Anwesenheitszeit.");
        if (day.TravelWorkTime > TimeSpan.Zero && !day.IsTravelDay)
            warnings.Add("Es ist Reisezeit eingetragen, aber Reisetag ist nicht aktiviert.");
        if (day.GrossTime > TimeSpan.FromHours(16))
            warnings.Add($"Die Anwesenheitszeit ist sehr lang: {TimeFormatter.Format(day.GrossTime)}.");
        if (day.ActualWorkTime > TimeSpan.FromHours(16))
            warnings.Add($"Die Istzeit ist sehr lang: {TimeFormatter.Format(day.ActualWorkTime)}.");
        if (day.TargetTime == TimeSpan.Zero)
            warnings.Add("Die Sollzeit ist 00:00.");

        return warnings;
    }
}

using System.Globalization;
using ReiseArbeitszeitApp.Helpers;

namespace ReiseArbeitszeitApp.Models;

public class TripEntry
{
    public int Id { get; set; }
    public string DepartureLocation { get; set; } = string.Empty;
    public string ArrivalLocation { get; set; } = string.Empty;
    public DateTime DepartureLocalDateTime { get; set; } = DateTime.Now;
    public DateTime ArrivalLocalDateTime { get; set; } = DateTime.Now;
    public string DepartureTimeZoneId { get; set; } = TimeZoneInfo.Local.Id;
    public string ArrivalTimeZoneId { get; set; } = TimeZoneInfo.Local.Id;
    public TimeSpan TravelTime { get; set; }
    public TimeSpan TimeDifference { get; set; }
    public string Note { get; set; } = string.Empty;

    public string DepartureText => DepartureLocalDateTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("de-DE"));
    public string ArrivalText => ArrivalLocalDateTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("de-DE"));
    public string TravelTimeText => TimeFormatter.Format(TravelTime);
    public string TimeDifferenceText => TimeFormatter.FormatSignedDuration(TimeDifference);
}
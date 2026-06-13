using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Services;

public class TimeCalculationService
{
    public TripEntry CalculateTrip(
        string departureLocation,
        string arrivalLocation,
        DateTime departureLocal,
        DateTime arrivalLocal,
        string departureTimeZoneId,
        string arrivalTimeZoneId,
        string note)
    {
        var departureZone = TimeZoneInfo.FindSystemTimeZoneById(departureTimeZoneId);
        var arrivalZone = TimeZoneInfo.FindSystemTimeZoneById(arrivalTimeZoneId);

        var departureUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(departureLocal, DateTimeKind.Unspecified), departureZone);
        var arrivalUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(arrivalLocal, DateTimeKind.Unspecified), arrivalZone);

        var departureOffset = departureZone.GetUtcOffset(departureLocal);
        var arrivalOffset = arrivalZone.GetUtcOffset(arrivalLocal);

        return new TripEntry
        {
            DepartureLocation = departureLocation.Trim(),
            ArrivalLocation = arrivalLocation.Trim(),
            DepartureLocalDateTime = departureLocal,
            ArrivalLocalDateTime = arrivalLocal,
            DepartureTimeZoneId = departureTimeZoneId,
            ArrivalTimeZoneId = arrivalTimeZoneId,
            TravelTime = arrivalUtc - departureUtc,
            TimeDifference = arrivalOffset - departureOffset,
            Note = note.Trim()
        };
    }
}

using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Sync;

public sealed record SyncWorkDay(
    Guid SyncId,
    DateTime Date,
    WorkDayType DayType,
    TimeSpan StartTime,
    TimeSpan EndTime,
    TimeSpan BreakTime,
    TimeSpan TargetTime,
    string CountryCode,
    string Location,
    string Note,
    bool IsTravelDay,
    TimeSpan TravelWorkTime,
    DateTimeOffset UpdatedAtUtc,
    bool IsDeleted = false);

public sealed record SyncTrip(
    Guid SyncId,
    string DepartureLocation,
    string ArrivalLocation,
    DateTime DepartureLocalDateTime,
    DateTime ArrivalLocalDateTime,
    string DepartureTimeZoneId,
    string ArrivalTimeZoneId,
    TimeSpan TravelTime,
    TimeSpan TimeDifference,
    string Note,
    DateTimeOffset UpdatedAtUtc,
    bool IsDeleted = false);

public sealed record SyncPushRequest(
    string DeviceId,
    IReadOnlyList<SyncWorkDay> WorkDays,
    IReadOnlyList<SyncTrip> Trips);

public sealed record SyncPushResponse(
    long Cursor,
    int AcceptedWorkDays,
    int AcceptedTrips,
    DateTimeOffset ServerTimeUtc);

public sealed record SyncPullResponse(
    long Cursor,
    IReadOnlyList<SyncWorkDay> WorkDays,
    IReadOnlyList<SyncTrip> Trips,
    DateTimeOffset ServerTimeUtc);

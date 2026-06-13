using ReiseArbeitszeitApp.Sync;

namespace ReiseArbeitszeitApp.SyncApi;

public sealed class InMemorySyncStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Versioned<SyncWorkDay>> _workDays = [];
    private readonly Dictionary<Guid, Versioned<SyncTrip>> _trips = [];
    private long _cursor;

    public SyncPushResponse Push(SyncPushRequest request)
    {
        lock (_gate)
        {
            var acceptedWorkDays = Merge(
                request.WorkDays,
                _workDays,
                item => item.SyncId,
                item => item.UpdatedAtUtc);
            var acceptedTrips = Merge(
                request.Trips,
                _trips,
                item => item.SyncId,
                item => item.UpdatedAtUtc);

            return new SyncPushResponse(
                _cursor,
                acceptedWorkDays,
                acceptedTrips,
                DateTimeOffset.UtcNow);
        }
    }

    public SyncPullResponse Pull(long cursor)
    {
        lock (_gate)
        {
            var workDays = _workDays.Values
                .Where(item => item.Cursor > cursor)
                .OrderBy(item => item.Cursor)
                .Select(item => item.Value)
                .ToList();
            var trips = _trips.Values
                .Where(item => item.Cursor > cursor)
                .OrderBy(item => item.Cursor)
                .Select(item => item.Value)
                .ToList();

            return new SyncPullResponse(
                _cursor,
                workDays,
                trips,
                DateTimeOffset.UtcNow);
        }
    }

    private int Merge<T>(
        IReadOnlyList<T> incoming,
        Dictionary<Guid, Versioned<T>> target,
        Func<T, Guid> getId,
        Func<T, DateTimeOffset> getUpdatedAtUtc)
    {
        var accepted = 0;
        foreach (var item in incoming)
        {
            var id = getId(item);
            if (id == Guid.Empty)
                continue;

            if (target.TryGetValue(id, out var current)
                && current.UpdatedAtUtc >= getUpdatedAtUtc(item))
            {
                continue;
            }

            target[id] = new Versioned<T>(
                item,
                getUpdatedAtUtc(item),
                ++_cursor);
            accepted++;
        }

        return accepted;
    }

    private sealed record Versioned<T>(
        T Value,
        DateTimeOffset UpdatedAtUtc,
        long Cursor);
}

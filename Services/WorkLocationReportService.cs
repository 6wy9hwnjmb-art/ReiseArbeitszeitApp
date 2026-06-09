using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Services;

public sealed class WorkLocationReportService
{
    public IReadOnlyList<WorkLocationReportRow> CreateCountryRows(
        IReadOnlyCollection<WorkDay> monthDays,
        IReadOnlyCollection<WorkDay> yearDays)
    {
        return CreateRows(
            monthDays,
            yearDays,
            day => string.IsNullOrWhiteSpace(day.CountryCode)
                ? "unknown"
                : day.CountryCode.Trim().ToUpperInvariant(),
            day => string.IsNullOrWhiteSpace(day.CountryCode)
                ? "Nicht angegeben"
                : day.CountryName);
    }

    public IReadOnlyList<WorkLocationReportRow> CreateLocationRows(
        IReadOnlyCollection<WorkDay> monthDays,
        IReadOnlyCollection<WorkDay> yearDays)
    {
        return CreateRows(
            monthDays,
            yearDays,
            day =>
                $"{Normalize(day.CountryCode)}|{Normalize(day.Location)}",
            day =>
            {
                var location = string.IsNullOrWhiteSpace(day.Location)
                    ? "Ohne Einsatzort"
                    : day.Location.Trim();
                var country = string.IsNullOrWhiteSpace(day.CountryCode)
                    ? "Nicht angegeben"
                    : day.CountryName;
                return $"{location} · {country}";
            });
    }

    private static IReadOnlyList<WorkLocationReportRow> CreateRows(
        IReadOnlyCollection<WorkDay> monthDays,
        IReadOnlyCollection<WorkDay> yearDays,
        Func<WorkDay, string> keySelector,
        Func<WorkDay, string> nameSelector)
    {
        var monthGroups = GroupWorkingDays(monthDays, keySelector);
        var yearGroups = GroupWorkingDays(yearDays, keySelector);

        return yearGroups
            .Select(group =>
            {
                monthGroups.TryGetValue(group.Key, out var monthGroup);
                var sample = group.Value[0];
                return new WorkLocationReportRow
                {
                    Name = nameSelector(sample),
                    MonthDayCount = monthGroup?.Count ?? 0,
                    MonthActualWorkTime = SumActualWorkTime(monthGroup),
                    YearDayCount = group.Value.Count,
                    YearActualWorkTime = SumActualWorkTime(group.Value)
                };
            })
            .OrderByDescending(row => row.MonthActualWorkTime)
            .ThenByDescending(row => row.YearActualWorkTime)
            .ThenBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, List<WorkDay>> GroupWorkingDays(
        IEnumerable<WorkDay> days,
        Func<WorkDay, string> keySelector)
    {
        return days
            .Where(day => !day.IsAbsence)
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static TimeSpan SumActualWorkTime(IReadOnlyCollection<WorkDay>? days)
    {
        return days?.Aggregate(TimeSpan.Zero, (sum, day) => sum + day.ActualWorkTime)
               ?? TimeSpan.Zero;
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value.Trim().ToUpperInvariant();
    }
}

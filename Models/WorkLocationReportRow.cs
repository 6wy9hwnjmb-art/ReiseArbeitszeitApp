using ReiseArbeitszeitApp.Helpers;

namespace ReiseArbeitszeitApp.Models;

public sealed class WorkLocationReportRow
{
    public string Name { get; init; } = string.Empty;
    public int MonthDayCount { get; init; }
    public TimeSpan MonthActualWorkTime { get; init; }
    public int YearDayCount { get; init; }
    public TimeSpan YearActualWorkTime { get; init; }

    public string MonthDayCountText => MonthDayCount.ToString();
    public string MonthActualWorkTimeText => TimeFormatter.FormatDuration(MonthActualWorkTime);
    public string YearDayCountText => YearDayCount.ToString();
    public string YearActualWorkTimeText => TimeFormatter.FormatDuration(YearActualWorkTime);
}

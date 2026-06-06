using System.Globalization;
using ReiseArbeitszeitApp.Helpers;

namespace ReiseArbeitszeitApp.Models;

public class WorkDay
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public TimeSpan StartTime { get; set; } = new(7, 0, 0);
    public TimeSpan EndTime { get; set; } = new(16, 0, 0);
    public TimeSpan BreakTime { get; set; } = new(0, 30, 0);
    public TimeSpan TargetTime { get; set; } = new(8, 0, 0);
    public string Location { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public bool IsTravelDay { get; set; }
    public TimeSpan TravelWorkTime { get; set; } = TimeSpan.Zero;

    public string DateText => Date.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));
    public string StartTimeText => TimeFormatter.FormatClock(StartTime);
    public string EndTimeText => TimeFormatter.FormatClock(EndTime);
    public string BreakTimeText => TimeFormatter.FormatDuration(BreakTime);
    public string TargetTimeText => TimeFormatter.FormatDuration(TargetTime);
    public string ActualWorkTimeText => TimeFormatter.FormatDuration(ActualWorkTime);
    public string OvertimeText => TimeFormatter.FormatSignedDuration(Overtime);
    public string TravelWorkTimeText => TimeFormatter.FormatDuration(TravelWorkTime);

    public TimeSpan GrossTime
    {
        get
        {
            var duration = EndTime - StartTime;
            if (duration < TimeSpan.Zero)
                duration += TimeSpan.FromDays(1);
            return duration;
        }
    }

    public TimeSpan ActualWorkTime
    {
        get
        {
            var result = GrossTime - BreakTime + TravelWorkTime;
            return result < TimeSpan.Zero ? TimeSpan.Zero : result;
        }
    }

    public TimeSpan Overtime => ActualWorkTime - TargetTime;
}
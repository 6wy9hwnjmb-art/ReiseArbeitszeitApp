namespace ReiseArbeitszeitApp.Helpers;

public static class TimeFormatter
{
    public static string Format(TimeSpan value)
    {
        var sign = value < TimeSpan.Zero ? "-" : "";
        value = value.Duration();
        return $"{sign}{(int)value.TotalHours:0} h {value.Minutes:00} min";
    }

    public static string FormatSigned(TimeSpan value)
    {
        if (value == TimeSpan.Zero)
            return "0 h 00 min";

        var sign = value < TimeSpan.Zero ? "-" : "+";
        value = value.Duration();
        return $"{sign}{(int)value.TotalHours:0} h {value.Minutes:00} min";
    }

    public static string FormatClock(TimeSpan value)
    {
        return $"{value.Hours:00}:{value.Minutes:00}";
    }

    public static string FormatDuration(TimeSpan value)
    {
        var sign = value < TimeSpan.Zero ? "-" : "";
        value = value.Duration();
        return $"{sign}{(int)value.TotalHours:00}:{value.Minutes:00}";
    }

    public static string FormatSignedDuration(TimeSpan value)
    {
        if (value == TimeSpan.Zero)
            return "00:00";

        var sign = value < TimeSpan.Zero ? "-" : "+";
        value = value.Duration();
        return $"{sign}{(int)value.TotalHours:00}:{value.Minutes:00}";
    }
}
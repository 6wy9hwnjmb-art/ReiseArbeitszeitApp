namespace ReiseArbeitszeitApp.Models;

public enum WorkDayType
{
    Work,
    HomeOffice,
    Vacation,
    Sick,
    Holiday
}

public static class WorkDayTypeInfo
{
    public static string GetDisplayName(WorkDayType type)
    {
        return type switch
        {
            WorkDayType.Work => "Arbeit",
            WorkDayType.HomeOffice => "Homeoffice",
            WorkDayType.Vacation => "Urlaub",
            WorkDayType.Sick => "Krankheit",
            WorkDayType.Holiday => "Feiertag",
            _ => "Arbeit"
        };
    }
}

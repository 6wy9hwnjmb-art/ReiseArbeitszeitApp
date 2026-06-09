using System.Globalization;

namespace ReiseArbeitszeitApp.Models;

public enum HolidayRuleKind
{
    CustomHoliday,
    DisabledAutomaticHoliday
}

public enum HolidayRuleScope
{
    HolidayRegion,
    Country,
    WorkLocation
}

public sealed class HolidayRule
{
    public int Id { get; set; }
    public HolidayRuleKind Kind { get; set; } = HolidayRuleKind.CustomHoliday;
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public bool IsRecurring { get; set; } = true;
    public HolidayRuleScope Scope { get; set; } = HolidayRuleScope.HolidayRegion;
    public string RegionCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public string DateText => IsRecurring
        ? Date.ToString("dd.MM.", CultureInfo.GetCultureInfo("de-DE"))
        : Date.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));

    public string KindText => Kind == HolidayRuleKind.DisabledAutomaticHoliday
        ? "Automatisch deaktiviert"
        : "Eigener Feiertag";

    public string ScopeText => Scope switch
    {
        HolidayRuleScope.Country => CountryCatalog.GetDisplayName(CountryCode),
        HolidayRuleScope.WorkLocation =>
            $"{Location} · {CountryCatalog.GetDisplayName(CountryCode)}",
        _ => HolidayRegionCatalog.FindByCode(RegionCode)?.DisplayName ?? RegionCode
    };

    public bool MatchesDate(DateTime date)
    {
        return IsRecurring
            ? Date.Month == date.Month && Date.Day == date.Day
            : Date.Date == date.Date;
    }
}

public sealed class HolidayOccurrence
{
    public DateTime Date { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsDisabled { get; init; }
    public string DateText => Date.ToString(
        "ddd, dd.MM.yyyy",
        CultureInfo.GetCultureInfo("de-DE"));
    public string StatusText => IsDisabled ? "Deaktiviert" : "Aktiv";
}

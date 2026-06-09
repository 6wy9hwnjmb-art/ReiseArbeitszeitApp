using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Services;

public sealed class HolidayService
{
    private readonly GermanHolidayService _germanService = new();
    private readonly SwissHolidayService _swissService = new();

    public string? GetHolidayName(
        DateTime date,
        AppSettings settings,
        IReadOnlyCollection<HolidayRule>? rules = null,
        string? countryCode = null,
        string? location = null)
    {
        return GetHolidays(date.Year, settings, rules, countryCode, location)
            .GetValueOrDefault(date.Date);
    }

    public IReadOnlyDictionary<DateTime, string> GetHolidays(
        int year,
        AppSettings settings,
        IReadOnlyCollection<HolidayRule>? rules = null,
        string? countryCode = null,
        string? location = null)
    {
        var automaticHolidays = GetAutomaticHolidays(year, settings);
        if (rules is null || rules.Count == 0)
            return automaticHolidays;

        var region = HolidayRegionCatalog.Resolve(settings);
        var result = automaticHolidays.ToDictionary(entry => entry.Key, entry => entry.Value);
        var applicableRules = rules
            .Where(rule => RuleMatchesScope(rule, region, countryCode, location))
            .ToList();

        foreach (var rule in applicableRules.Where(rule =>
                     rule.Kind == HolidayRuleKind.DisabledAutomaticHoliday))
        {
            foreach (var holiday in result
                         .Where(entry => rule.MatchesDate(entry.Key)
                                         && string.Equals(
                                             entry.Value,
                                             rule.Name,
                                             StringComparison.CurrentCultureIgnoreCase))
                         .ToList())
            {
                result.Remove(holiday.Key);
            }
        }

        foreach (var rule in applicableRules.Where(rule =>
                     rule.Kind == HolidayRuleKind.CustomHoliday))
        {
            var date = ResolveRuleDate(rule, year);
            if (date is not null)
                result[date.Value] = rule.Name;
        }

        return result;
    }

    public IReadOnlyDictionary<DateTime, string> GetAutomaticHolidays(
        int year,
        AppSettings settings)
    {
        var region = HolidayRegionCatalog.Resolve(settings);
        if (region.Country == HolidayCountry.Switzerland
            && region.SwissCanton is SwissCanton canton)
        {
            return _swissService.GetHolidays(year, canton);
        }

        return _germanService.GetHolidays(
            year,
            region.GermanState ?? GermanFederalState.Hessen);
    }

    public HolidayRegion GetRegion(AppSettings settings)
    {
        return HolidayRegionCatalog.Resolve(settings);
    }

    public string GetRegionNotice(AppSettings settings)
    {
        var region = GetRegion(settings);
        if (region.Country != HolidayCountry.Switzerland)
            return string.Empty;

        return region.HasLocalVariations
            ? "Es werden nur kantonsweit geltende Feiertage berücksichtigt; je nach Bezirk oder Gemeinde bestehen Abweichungen."
            : "Vertragliche und lokale Sonderregelungen können zusätzlich gelten.";
    }

    private static bool RuleMatchesScope(
        HolidayRule rule,
        HolidayRegion region,
        string? countryCode,
        string? location)
    {
        return rule.Scope switch
        {
            HolidayRuleScope.Country => string.Equals(
                rule.CountryCode,
                string.IsNullOrWhiteSpace(countryCode)
                    ? GetCountryCode(region)
                    : countryCode,
                StringComparison.OrdinalIgnoreCase),
            HolidayRuleScope.WorkLocation =>
                !string.IsNullOrWhiteSpace(location)
                && string.Equals(rule.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    rule.Location.Trim(),
                    location.Trim(),
                    StringComparison.CurrentCultureIgnoreCase),
            _ => string.Equals(rule.RegionCode, region.Code, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static DateTime? ResolveRuleDate(HolidayRule rule, int year)
    {
        if (!rule.IsRecurring)
            return rule.Date.Year == year ? rule.Date.Date : null;

        return DateTime.DaysInMonth(year, rule.Date.Month) < rule.Date.Day
            ? null
            : new DateTime(year, rule.Date.Month, rule.Date.Day);
    }

    private static string GetCountryCode(HolidayRegion region)
    {
        return region.Country == HolidayCountry.Switzerland ? "CH" : "DE";
    }
}

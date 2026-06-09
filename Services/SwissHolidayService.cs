using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Services;

public sealed class SwissHolidayService
{
    public IReadOnlyDictionary<DateTime, string> GetHolidays(int year, SwissCanton canton)
    {
        var easterSunday = CalculateEasterSunday(year);
        var holidays = new Dictionary<DateTime, string>
        {
            [new DateTime(year, 1, 1)] = "Neujahr",
            [easterSunday.AddDays(39)] = "Auffahrt",
            [new DateTime(year, 8, 1)] = "Bundesfeier",
            [new DateTime(year, 12, 25)] = "Weihnachten"
        };

        AddFixed(holidays, year, 1, 2, "Berchtoldstag", canton,
            SwissCanton.Bern, SwissCanton.Schaffhausen,
            SwissCanton.Thurgau, SwissCanton.Vaud);
        AddFixed(holidays, year, 1, 6, "Heilige Drei Könige", canton,
            SwissCanton.Ticino, SwissCanton.Uri, SwissCanton.Schwyz);
        AddFixed(holidays, year, 3, 1, "Ausrufung der Republik", canton,
            SwissCanton.Neuchatel);
        AddFixed(holidays, year, 3, 19, "Josefstag", canton,
            SwissCanton.Schwyz, SwissCanton.Valais, SwissCanton.Lucerne,
            SwissCanton.Uri, SwissCanton.Nidwalden, SwissCanton.Ticino);

        AddRelative(holidays, easterSunday, -2, "Karfreitag", canton,
            SwissCanton.Zurich, SwissCanton.Bern, SwissCanton.Lucerne, SwissCanton.Uri,
            SwissCanton.Schwyz, SwissCanton.Obwalden, SwissCanton.Nidwalden,
            SwissCanton.Glarus, SwissCanton.Zug, SwissCanton.Fribourg,
            SwissCanton.Solothurn, SwissCanton.BaselStadt, SwissCanton.BaselLandschaft,
            SwissCanton.Schaffhausen, SwissCanton.AppenzellAusserrhoden,
            SwissCanton.AppenzellInnerrhoden, SwissCanton.StGallen,
            SwissCanton.Graubuenden, SwissCanton.Aargau, SwissCanton.Thurgau,
            SwissCanton.Vaud, SwissCanton.Neuchatel, SwissCanton.Geneva, SwissCanton.Jura);

        AddRelative(holidays, easterSunday, 1, "Ostermontag", canton,
            SwissCanton.Zurich, SwissCanton.Bern, SwissCanton.Glarus,
            SwissCanton.BaselStadt, SwissCanton.BaselLandschaft,
            SwissCanton.Schaffhausen, SwissCanton.AppenzellAusserrhoden,
            SwissCanton.AppenzellInnerrhoden, SwissCanton.StGallen,
            SwissCanton.Graubuenden, SwissCanton.Thurgau,
            SwissCanton.Ticino, SwissCanton.Vaud, SwissCanton.Geneva, SwissCanton.Jura,
            SwissCanton.Uri, SwissCanton.Schwyz, SwissCanton.Obwalden);

        AddFixed(holidays, year, 5, 1, "Tag der Arbeit", canton,
            SwissCanton.Zurich, SwissCanton.BaselStadt,
            SwissCanton.BaselLandschaft, SwissCanton.Schaffhausen,
            SwissCanton.Neuchatel, SwissCanton.Jura, SwissCanton.Thurgau, SwissCanton.Ticino);

        AddRelative(holidays, easterSunday, 50, "Pfingstmontag", canton,
            SwissCanton.Zurich, SwissCanton.Bern, SwissCanton.Glarus,
            SwissCanton.BaselStadt, SwissCanton.BaselLandschaft,
            SwissCanton.Schaffhausen, SwissCanton.AppenzellAusserrhoden,
            SwissCanton.AppenzellInnerrhoden, SwissCanton.StGallen,
            SwissCanton.Graubuenden, SwissCanton.Thurgau,
            SwissCanton.Ticino, SwissCanton.Vaud, SwissCanton.Geneva, SwissCanton.Jura,
            SwissCanton.Uri, SwissCanton.Schwyz, SwissCanton.Obwalden);

        AddRelative(holidays, easterSunday, 60, "Fronleichnam", canton,
            SwissCanton.Lucerne, SwissCanton.Uri, SwissCanton.Schwyz,
            SwissCanton.Obwalden, SwissCanton.Nidwalden, SwissCanton.Zug,
            SwissCanton.AppenzellInnerrhoden,
            SwissCanton.Valais, SwissCanton.Jura, SwissCanton.Ticino);

        AddFixed(holidays, year, 6, 23, "Jurassischer Volksentscheid", canton, SwissCanton.Jura);
        AddFixed(holidays, year, 6, 29, "Peter und Paul", canton, SwissCanton.Ticino);
        AddFixed(holidays, year, 8, 15, "Mariä Himmelfahrt", canton,
            SwissCanton.Lucerne, SwissCanton.Uri, SwissCanton.Schwyz,
            SwissCanton.Obwalden, SwissCanton.Nidwalden, SwissCanton.Zug,
            SwissCanton.Ticino, SwissCanton.Valais,
            SwissCanton.Jura);

        AddFixed(holidays, year, 11, 1, "Allerheiligen", canton,
            SwissCanton.Lucerne, SwissCanton.Uri, SwissCanton.Schwyz,
            SwissCanton.Obwalden, SwissCanton.Nidwalden, SwissCanton.Glarus,
            SwissCanton.Zug, SwissCanton.StGallen, SwissCanton.Ticino,
            SwissCanton.Valais, SwissCanton.Jura);
        AddFixed(holidays, year, 12, 8, "Mariä Empfängnis", canton,
            SwissCanton.Uri, SwissCanton.Obwalden, SwissCanton.Nidwalden,
            SwissCanton.Zug, SwissCanton.Valais,
            SwissCanton.Jura, SwissCanton.Lucerne, SwissCanton.Schwyz, SwissCanton.Ticino);
        AddFixed(holidays, year, 12, 26, "Stephanstag", canton,
            SwissCanton.Zurich, SwissCanton.Bern, SwissCanton.Lucerne,
            SwissCanton.Glarus, SwissCanton.BaselStadt,
            SwissCanton.BaselLandschaft, SwissCanton.Schaffhausen,
            SwissCanton.StGallen, SwissCanton.Graubuenden,
            SwissCanton.Thurgau, SwissCanton.Ticino, SwissCanton.Uri,
            SwissCanton.Schwyz, SwissCanton.Obwalden);
        AddFixed(holidays, year, 12, 31, "Wiederherstellung der Republik", canton,
            SwissCanton.Geneva);

        if (canton == SwissCanton.Glarus)
            holidays[CalculateNaefelserFahrt(year, easterSunday)] = "Näfelser Fahrt";
        if (canton == SwissCanton.Geneva)
            holidays[CalculateGenevaFast(year)] = "Genfer Bettag";
        if (canton == SwissCanton.Vaud)
            holidays[CalculateFederalFastMonday(year)] = "Bettagsmontag";
        if (canton == SwissCanton.AppenzellInnerrhoden
            && new DateTime(year, 12, 25).DayOfWeek is not DayOfWeek.Friday and not DayOfWeek.Sunday)
        {
            holidays[new DateTime(year, 12, 26)] = "Stephanstag";
        }
        if (canton == SwissCanton.AppenzellAusserrhoden
            && new DateTime(year, 12, 25).DayOfWeek is not DayOfWeek.Monday and not DayOfWeek.Friday)
        {
            holidays[new DateTime(year, 12, 26)] = "Stephanstag";
        }
        if (canton == SwissCanton.Neuchatel)
        {
            if (new DateTime(year, 1, 1).DayOfWeek == DayOfWeek.Sunday)
                holidays[new DateTime(year, 1, 2)] = "2. Januar";
            if (new DateTime(year, 12, 25).DayOfWeek == DayOfWeek.Sunday)
                holidays[new DateTime(year, 12, 26)] = "Stephanstag";
        }

        return holidays;
    }

    private static void AddFixed(
        IDictionary<DateTime, string> holidays,
        int year,
        int month,
        int day,
        string name,
        SwissCanton selected,
        params SwissCanton[] cantons)
    {
        if (cantons.Contains(selected))
            holidays[new DateTime(year, month, day)] = name;
    }

    private static void AddRelative(
        IDictionary<DateTime, string> holidays,
        DateTime easterSunday,
        int offset,
        string name,
        SwissCanton selected,
        params SwissCanton[] cantons)
    {
        if (cantons.Contains(selected))
            holidays[easterSunday.AddDays(offset)] = name;
    }

    private static DateTime CalculateNaefelserFahrt(int year, DateTime easterSunday)
    {
        var date = new DateTime(year, 4, 1);
        while (date.DayOfWeek != DayOfWeek.Thursday)
            date = date.AddDays(1);

        var palmSunday = easterSunday.AddDays(-7);
        if (date >= palmSunday && date <= easterSunday)
            date = date.AddDays(7);
        return date;
    }

    private static DateTime CalculateGenevaFast(int year)
    {
        var firstSunday = new DateTime(year, 9, 1);
        while (firstSunday.DayOfWeek != DayOfWeek.Sunday)
            firstSunday = firstSunday.AddDays(1);
        return firstSunday.AddDays(4);
    }

    private static DateTime CalculateFederalFastMonday(int year)
    {
        var date = new DateTime(year, 9, 1);
        var sundayCount = 0;
        while (true)
        {
            if (date.DayOfWeek == DayOfWeek.Sunday && ++sundayCount == 3)
                return date.AddDays(1);
            date = date.AddDays(1);
        }
    }

    private static DateTime CalculateEasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateTime(year, month, day);
    }
}

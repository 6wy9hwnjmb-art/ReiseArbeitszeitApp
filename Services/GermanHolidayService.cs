using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Services;

public class GermanHolidayService
{
    public string? GetHolidayName(DateTime date, GermanFederalState state)
    {
        return GetHolidays(date.Year, state).GetValueOrDefault(date.Date);
    }

    public IReadOnlyDictionary<DateTime, string> GetHolidays(int year, GermanFederalState state)
    {
        var easterSunday = CalculateEasterSunday(year);
        var holidays = new Dictionary<DateTime, string>
        {
            [new DateTime(year, 1, 1)] = "Neujahr",
            [easterSunday.AddDays(-2)] = "Karfreitag",
            [easterSunday.AddDays(1)] = "Ostermontag",
            [new DateTime(year, 5, 1)] = "Tag der Arbeit",
            [easterSunday.AddDays(39)] = "Christi Himmelfahrt",
            [easterSunday.AddDays(50)] = "Pfingstmontag",
            [new DateTime(year, 10, 3)] = "Tag der Deutschen Einheit",
            [new DateTime(year, 12, 25)] = "1. Weihnachtstag",
            [new DateTime(year, 12, 26)] = "2. Weihnachtstag"
        };

        if (state is GermanFederalState.BadenWuerttemberg
            or GermanFederalState.Bayern
            or GermanFederalState.SachsenAnhalt)
        {
            holidays[new DateTime(year, 1, 6)] = "Heilige Drei Könige";
        }

        if (state is GermanFederalState.Berlin or GermanFederalState.MecklenburgVorpommern)
            holidays[new DateTime(year, 3, 8)] = "Internationaler Frauentag";

        if (state is GermanFederalState.BadenWuerttemberg
            or GermanFederalState.Bayern
            or GermanFederalState.Hessen
            or GermanFederalState.NordrheinWestfalen
            or GermanFederalState.RheinlandPfalz
            or GermanFederalState.Saarland)
        {
            holidays[easterSunday.AddDays(60)] = "Fronleichnam";
        }

        if (state == GermanFederalState.Saarland)
            holidays[new DateTime(year, 8, 15)] = "Mariä Himmelfahrt";

        if (state == GermanFederalState.Thueringen)
            holidays[new DateTime(year, 9, 20)] = "Weltkindertag";

        if (state is GermanFederalState.Brandenburg
            or GermanFederalState.Bremen
            or GermanFederalState.Hamburg
            or GermanFederalState.MecklenburgVorpommern
            or GermanFederalState.Niedersachsen
            or GermanFederalState.Sachsen
            or GermanFederalState.SachsenAnhalt
            or GermanFederalState.SchleswigHolstein
            or GermanFederalState.Thueringen)
        {
            holidays[new DateTime(year, 10, 31)] = "Reformationstag";
        }

        if (state is GermanFederalState.BadenWuerttemberg
            or GermanFederalState.Bayern
            or GermanFederalState.NordrheinWestfalen
            or GermanFederalState.RheinlandPfalz
            or GermanFederalState.Saarland)
        {
            holidays[new DateTime(year, 11, 1)] = "Allerheiligen";
        }

        if (state == GermanFederalState.Sachsen)
            holidays[CalculateDayOfRepentance(year)] = "Buß- und Bettag";

        return holidays;
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

    private static DateTime CalculateDayOfRepentance(int year)
    {
        var date = new DateTime(year, 11, 22);
        while (date.DayOfWeek != DayOfWeek.Wednesday)
            date = date.AddDays(-1);
        return date;
    }
}

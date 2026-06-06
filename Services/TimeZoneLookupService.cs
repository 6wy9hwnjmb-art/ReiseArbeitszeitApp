using System.Globalization;
using System.Text;

namespace ReiseArbeitszeitApp.Services;

public class TimeZoneLookupService
{
    private static readonly IReadOnlyDictionary<string, string> LocationTimeZones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["amsterdam"] = "W. Europe Standard Time",
        ["atlanta"] = "Eastern Standard Time",
        ["austin"] = "Central Standard Time",
        ["bangkok"] = "SE Asia Standard Time",
        ["barcelona"] = "Romance Standard Time",
        ["beijing"] = "China Standard Time",
        ["berlin"] = "W. Europe Standard Time",
        ["boston"] = "Eastern Standard Time",
        ["brüssel"] = "Romance Standard Time",
        ["brussels"] = "Romance Standard Time",
        ["chicago"] = "Central Standard Time",
        ["dallas"] = "Central Standard Time",
        ["dfw"] = "Central Standard Time",
        ["denver"] = "Mountain Standard Time",
        ["dubai"] = "Arabian Standard Time",
        ["dublin"] = "GMT Standard Time",
        ["düsseldorf"] = "W. Europe Standard Time",
        ["duesseldorf"] = "W. Europe Standard Time",
        ["frankfurt"] = "W. Europe Standard Time",
        ["fra"] = "W. Europe Standard Time",
        ["ham"] = "W. Europe Standard Time",
        ["hamburg"] = "W. Europe Standard Time",
        ["helsinki"] = "FLE Standard Time",
        ["hong kong"] = "China Standard Time",
        ["istanbul"] = "Turkey Standard Time",
        ["johannesburg"] = "South Africa Standard Time",
        ["kopenhagen"] = "Romance Standard Time",
        ["copenhagen"] = "Romance Standard Time",
        ["las vegas"] = "Pacific Standard Time",
        ["lax"] = "Pacific Standard Time",
        ["london"] = "GMT Standard Time",
        ["los angeles"] = "Pacific Standard Time",
        ["madrid"] = "Romance Standard Time",
        ["mexico city"] = "Central Standard Time (Mexico)",
        ["miami"] = "Eastern Standard Time",
        ["münchen"] = "W. Europe Standard Time",
        ["munich"] = "W. Europe Standard Time",
        ["new york"] = "Eastern Standard Time",
        ["nyc"] = "Eastern Standard Time",
        ["paris"] = "Romance Standard Time",
        ["phoenix"] = "US Mountain Standard Time",
        ["prag"] = "Central Europe Standard Time",
        ["prague"] = "Central Europe Standard Time",
        ["rio de janeiro"] = "E. South America Standard Time",
        ["rom"] = "W. Europe Standard Time",
        ["rome"] = "W. Europe Standard Time",
        ["san francisco"] = "Pacific Standard Time",
        ["seattle"] = "Pacific Standard Time",
        ["singapur"] = "Singapore Standard Time",
        ["singapore"] = "Singapore Standard Time",
        ["stockholm"] = "W. Europe Standard Time",
        ["sydney"] = "AUS Eastern Standard Time",
        ["tokio"] = "Tokyo Standard Time",
        ["tokyo"] = "Tokyo Standard Time",
        ["toronto"] = "Eastern Standard Time",
        ["vancouver"] = "Pacific Standard Time",
        ["warschau"] = "Central European Standard Time",
        ["warsaw"] = "Central European Standard Time",
        ["washington"] = "Eastern Standard Time",
        ["wien"] = "W. Europe Standard Time",
        ["vienna"] = "W. Europe Standard Time",
        ["zürich"] = "W. Europe Standard Time",
        ["zurich"] = "W. Europe Standard Time"
    };

    private static readonly IReadOnlyDictionary<string, string> CountryTimeZones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["deutschland"] = "W. Europe Standard Time",
        ["germany"] = "W. Europe Standard Time",
        ["frankreich"] = "Romance Standard Time",
        ["france"] = "Romance Standard Time",
        ["großbritannien"] = "GMT Standard Time",
        ["great britain"] = "GMT Standard Time",
        ["england"] = "GMT Standard Time",
        ["italien"] = "W. Europe Standard Time",
        ["italy"] = "W. Europe Standard Time",
        ["niederlande"] = "W. Europe Standard Time",
        ["netherlands"] = "W. Europe Standard Time",
        ["schweiz"] = "W. Europe Standard Time",
        ["switzerland"] = "W. Europe Standard Time",
        ["spanien"] = "Romance Standard Time",
        ["spain"] = "Romance Standard Time",
        ["usa ostküste"] = "Eastern Standard Time",
        ["usa westküste"] = "Pacific Standard Time",
        ["texas"] = "Central Standard Time"
    };

    public TimeZoneInfo? FindByLocation(string location)
    {
        var normalized = Normalize(location);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var timeZoneId = FindTimeZoneId(normalized);
        return timeZoneId is null ? null : FindSystemTimeZone(timeZoneId);
    }

    private static string? FindTimeZoneId(string normalizedLocation)
    {
        foreach (var entry in LocationTimeZones)
        {
            if (normalizedLocation.Contains(Normalize(entry.Key), StringComparison.OrdinalIgnoreCase))
                return entry.Value;
        }

        foreach (var entry in CountryTimeZones)
        {
            if (normalizedLocation.Contains(Normalize(entry.Key), StringComparison.OrdinalIgnoreCase))
                return entry.Value;
        }

        return null;
    }

    private static TimeZoneInfo? FindSystemTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    private static string Normalize(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
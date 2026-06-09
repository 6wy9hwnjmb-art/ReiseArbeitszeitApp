using System.Globalization;

namespace ReiseArbeitszeitApp.Models;

public sealed record CountryOption(string Code, string Name)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Code)
        ? Name
        : $"{Name} ({Code})";

    public override string ToString() => DisplayName;
}

public static class CountryCatalog
{
    private static readonly IReadOnlyList<CountryOption> Countries = BuildCountries();

    public static IReadOnlyList<CountryOption> GetAll() => Countries;

    public static CountryOption Resolve(string? code)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            var country = Countries.FirstOrDefault(item =>
                string.Equals(item.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
            if (country is not null)
                return country;
        }

        return Countries[0];
    }

    public static string GetDisplayName(string? code)
    {
        return Resolve(code).Name;
    }

    private static IReadOnlyList<CountryOption> BuildCountries()
    {
        var countries = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(TryCreateRegion)
            .Where(region => region is not null)
            .Cast<RegionInfo>()
            .Where(region => region.TwoLetterISORegionName.Length == 2)
            .GroupBy(region => region.TwoLetterISORegionName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(region => new CountryOption(
                region.TwoLetterISORegionName.ToUpperInvariant(),
                region.DisplayName))
            .OrderBy(country => country.Name, StringComparer.Create(
                CultureInfo.GetCultureInfo("de-DE"),
                ignoreCase: true))
            .ToList();

        countries.Insert(0, new CountryOption(string.Empty, "Nicht angegeben"));
        return countries;
    }

    private static RegionInfo? TryCreateRegion(CultureInfo culture)
    {
        try
        {
            return new RegionInfo(culture.Name);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

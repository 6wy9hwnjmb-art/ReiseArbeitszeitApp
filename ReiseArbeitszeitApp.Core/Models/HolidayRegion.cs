namespace ReiseArbeitszeitApp.Models;

public enum HolidayCountry
{
    Germany,
    Switzerland
}

public sealed record HolidayRegion(
    string Code,
    HolidayCountry Country,
    string RegionName,
    GermanFederalState? GermanState = null,
    SwissCanton? SwissCanton = null,
    bool HasLocalVariations = false)
{
    public string CountryName => Country == HolidayCountry.Switzerland ? "Schweiz" : "Deutschland";
    public string DisplayName => $"{CountryName} · {RegionName}";
    public string ShortDisplayName => Country == HolidayCountry.Switzerland
        ? $"Kanton {RegionName}"
        : RegionName;

    public override string ToString() => DisplayName;
}

public static class HolidayRegionCatalog
{
    private static readonly IReadOnlyList<HolidayRegion> Regions = BuildRegions();

    public static IReadOnlyList<HolidayRegion> GetAll() => Regions;

    public static HolidayRegion? FindByCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? null
            : Regions.FirstOrDefault(region =>
                string.Equals(region.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    public static HolidayRegion Resolve(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.HolidayRegionCode))
        {
            var selected = FindByCode(settings.HolidayRegionCode);
            if (selected is not null)
                return selected;
        }

        var legacyState = GermanFederalStateInfo.ParseOrDefault(settings.FederalState);
        return Regions.First(region => region.GermanState == legacyState);
    }

    private static IReadOnlyList<HolidayRegion> BuildRegions()
    {
        var regions = new List<HolidayRegion>();

        foreach (var state in Enum.GetValues<GermanFederalState>())
        {
            regions.Add(new HolidayRegion(
                $"DE:{state}",
                HolidayCountry.Germany,
                GermanFederalStateInfo.GetDisplayName(state),
                GermanState: state));
        }

        var cantonsWithLocalVariations = new HashSet<SwissCanton>
        {
            SwissCanton.Aargau,
            SwissCanton.AppenzellInnerrhoden,
            SwissCanton.Fribourg,
            SwissCanton.Neuchatel,
            SwissCanton.Solothurn
        };

        foreach (var canton in Enum.GetValues<SwissCanton>()
                     .OrderBy(SwissCantonInfo.GetDisplayName))
        {
            regions.Add(new HolidayRegion(
                $"CH:{SwissCantonInfo.GetCode(canton)}",
                HolidayCountry.Switzerland,
                SwissCantonInfo.GetDisplayName(canton),
                SwissCanton: canton,
                HasLocalVariations: cantonsWithLocalVariations.Contains(canton)));
        }

        return regions;
    }
}

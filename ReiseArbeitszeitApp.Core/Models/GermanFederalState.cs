namespace ReiseArbeitszeitApp.Models;

public enum GermanFederalState
{
    BadenWuerttemberg,
    Bayern,
    Berlin,
    Brandenburg,
    Bremen,
    Hamburg,
    Hessen,
    MecklenburgVorpommern,
    Niedersachsen,
    NordrheinWestfalen,
    RheinlandPfalz,
    Saarland,
    Sachsen,
    SachsenAnhalt,
    SchleswigHolstein,
    Thueringen
}

public static class GermanFederalStateInfo
{
    public static string GetDisplayName(GermanFederalState state)
    {
        return state switch
        {
            GermanFederalState.BadenWuerttemberg => "Baden-Württemberg",
            GermanFederalState.Bayern => "Bayern",
            GermanFederalState.Berlin => "Berlin",
            GermanFederalState.Brandenburg => "Brandenburg",
            GermanFederalState.Bremen => "Bremen",
            GermanFederalState.Hamburg => "Hamburg",
            GermanFederalState.Hessen => "Hessen",
            GermanFederalState.MecklenburgVorpommern => "Mecklenburg-Vorpommern",
            GermanFederalState.Niedersachsen => "Niedersachsen",
            GermanFederalState.NordrheinWestfalen => "Nordrhein-Westfalen",
            GermanFederalState.RheinlandPfalz => "Rheinland-Pfalz",
            GermanFederalState.Saarland => "Saarland",
            GermanFederalState.Sachsen => "Sachsen",
            GermanFederalState.SachsenAnhalt => "Sachsen-Anhalt",
            GermanFederalState.SchleswigHolstein => "Schleswig-Holstein",
            GermanFederalState.Thueringen => "Thüringen",
            _ => "Hessen"
        };
    }

    public static GermanFederalState ParseOrDefault(string? value)
    {
        return Enum.TryParse<GermanFederalState>(value, ignoreCase: true, out var state)
            ? state
            : GermanFederalState.Hessen;
    }
}

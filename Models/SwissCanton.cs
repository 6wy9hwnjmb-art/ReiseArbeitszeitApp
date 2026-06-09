namespace ReiseArbeitszeitApp.Models;

public enum SwissCanton
{
    Aargau,
    AppenzellInnerrhoden,
    AppenzellAusserrhoden,
    Bern,
    BaselLandschaft,
    BaselStadt,
    Fribourg,
    Geneva,
    Glarus,
    Graubuenden,
    Jura,
    Lucerne,
    Neuchatel,
    Nidwalden,
    Obwalden,
    StGallen,
    Schaffhausen,
    Solothurn,
    Schwyz,
    Thurgau,
    Ticino,
    Uri,
    Vaud,
    Valais,
    Zug,
    Zurich
}

public static class SwissCantonInfo
{
    public static string GetCode(SwissCanton canton)
    {
        return canton switch
        {
            SwissCanton.Aargau => "AG",
            SwissCanton.AppenzellInnerrhoden => "AI",
            SwissCanton.AppenzellAusserrhoden => "AR",
            SwissCanton.Bern => "BE",
            SwissCanton.BaselLandschaft => "BL",
            SwissCanton.BaselStadt => "BS",
            SwissCanton.Fribourg => "FR",
            SwissCanton.Geneva => "GE",
            SwissCanton.Glarus => "GL",
            SwissCanton.Graubuenden => "GR",
            SwissCanton.Jura => "JU",
            SwissCanton.Lucerne => "LU",
            SwissCanton.Neuchatel => "NE",
            SwissCanton.Nidwalden => "NW",
            SwissCanton.Obwalden => "OW",
            SwissCanton.StGallen => "SG",
            SwissCanton.Schaffhausen => "SH",
            SwissCanton.Solothurn => "SO",
            SwissCanton.Schwyz => "SZ",
            SwissCanton.Thurgau => "TG",
            SwissCanton.Ticino => "TI",
            SwissCanton.Uri => "UR",
            SwissCanton.Vaud => "VD",
            SwissCanton.Valais => "VS",
            SwissCanton.Zug => "ZG",
            SwissCanton.Zurich => "ZH",
            _ => "ZH"
        };
    }

    public static string GetDisplayName(SwissCanton canton)
    {
        return canton switch
        {
            SwissCanton.AppenzellInnerrhoden => "Appenzell Innerrhoden",
            SwissCanton.AppenzellAusserrhoden => "Appenzell Ausserrhoden",
            SwissCanton.BaselLandschaft => "Basel-Landschaft",
            SwissCanton.BaselStadt => "Basel-Stadt",
            SwissCanton.Fribourg => "Freiburg",
            SwissCanton.Geneva => "Genf",
            SwissCanton.Graubuenden => "Graubünden",
            SwissCanton.Lucerne => "Luzern",
            SwissCanton.Neuchatel => "Neuenburg",
            SwissCanton.StGallen => "St. Gallen",
            SwissCanton.Ticino => "Tessin",
            SwissCanton.Vaud => "Waadt",
            SwissCanton.Valais => "Wallis",
            SwissCanton.Zurich => "Zürich",
            _ => canton.ToString()
        };
    }
}

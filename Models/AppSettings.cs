namespace ReiseArbeitszeitApp.Models;

public class AppSettings
{
    public string DefaultDepartureLocation { get; set; } = "Frankfurt";
    public string DefaultWorkStart { get; set; } = "07:00";
    public string DefaultWorkEnd { get; set; } = "16:00";
    public string DefaultBreak { get; set; } = "00:30";
    public string DefaultTarget { get; set; } = "08:00";
    public string FederalState { get; set; } = nameof(GermanFederalState.Hessen);
    public string CsvExportFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public bool AutomaticBackupsEnabled { get; set; } = true;
}

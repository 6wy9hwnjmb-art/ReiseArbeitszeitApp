namespace ReiseArbeitszeitApp.Models;

public sealed record UpdateInfo(
    Version Version,
    string Name,
    string Notes,
    string DownloadUrl);


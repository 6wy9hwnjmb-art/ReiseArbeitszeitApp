namespace ReiseArbeitszeitApp.Services;

public static class UpdateConfiguration
{
    public const string GitHubOwner = "6wy9hwnjmb-art";
    public const string GitHubRepository = "ReiseArbeitszeitApp";

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(GitHubOwner);
}

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Services;

public sealed class UpdateService
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public async Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        if (!UpdateConfiguration.IsConfigured)
            return null;

        var requestUrl =
            $"https://api.github.com/repos/{UpdateConfiguration.GitHubOwner}/{UpdateConfiguration.GitHubRepository}/releases/latest";

        using var response = await HttpClient.GetAsync(requestUrl, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V');
        if (!Version.TryParse(tag, out var releaseVersion) || releaseVersion <= currentVersion)
            return null;

        var assets = root.GetProperty("assets").EnumerateArray();
        foreach (var asset in assets)
        {
            var name = asset.GetProperty("name").GetString() ?? string.Empty;
            if (!name.StartsWith("ReiseArbeitszeitApp-Setup-", StringComparison.OrdinalIgnoreCase)
                || !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var downloadUrl = asset.GetProperty("browser_download_url").GetString();
            if (string.IsNullOrWhiteSpace(downloadUrl))
                continue;

            return new UpdateInfo(
                releaseVersion,
                root.GetProperty("name").GetString() ?? $"Version {releaseVersion}",
                root.GetProperty("body").GetString() ?? string.Empty,
                downloadUrl);
        }

        return null;
    }

    public async Task<string> DownloadInstallerAsync(UpdateInfo update, CancellationToken cancellationToken = default)
    {
        var updateFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReiseArbeitszeitApp",
            "Updates");
        Directory.CreateDirectory(updateFolder);

        var installerPath = Path.Combine(updateFolder, $"ReiseArbeitszeitApp-Setup-{update.Version}.exe");

        using var response = await HttpClient.GetAsync(
            update.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = File.Create(installerPath);
        await source.CopyToAsync(destination, cancellationToken);

        return installerPath;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ReiseArbeitszeitApp", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Meow.Core.Models;

namespace Meow.Core.Services;

/// <summary>
/// Minimal PurrNet client implementation using HttpClient.
/// </summary>
public class PurrNetService : IPurrNetService
{
    private readonly HttpClient _http;

    public PurrNetService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<PurrSearchResult> SearchPackagesAsync(string query, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var url = $"packages?search={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}";
        var result = await _http.GetFromJsonAsync<PurrSearchResult>(url, ct).ConfigureAwait(false);
        return result ?? new PurrSearchResult();
    }

    public async Task<PurrPackage?> GetPackageAsync(string name, string? version = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Package name is required", nameof(name));

        var path = string.IsNullOrWhiteSpace(version) ? $"packages/{Uri.EscapeDataString(name)}" : $"packages/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(version)}";
        return await _http.GetFromJsonAsync<PurrPackage?>(path, ct).ConfigureAwait(false);
    }

    public async Task ReportDownloadAsync(string name, string version, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Package name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Package version is required", nameof(version));

        var path = $"packages/{Uri.EscapeDataString(name)}/download";
        // Body can be empty or include metadata; use empty object for clarity
        var resp = await _http.PostAsJsonAsync(path, new { version }, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }
}

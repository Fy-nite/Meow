using System.Threading;
using System.Threading.Tasks;
using Meow.Core.Models;

namespace Meow.Core.Services;

public interface IPurrNetService
{
    Task<PurrSearchResult> SearchPackagesAsync(string query, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<PurrPackage?> GetPackageAsync(string name, string? version = null, CancellationToken ct = default);
    Task ReportDownloadAsync(string name, string version, CancellationToken ct = default);
}

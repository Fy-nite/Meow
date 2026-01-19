using System.Threading;
using System.Threading.Tasks;

namespace Meow.Core.Services;

public interface IInstallService
{
    Task<bool> InstallAsync(string projectPath, bool saveCategories = true, CancellationToken ct = default);
    Task<bool> UpdateAsync(string projectPath, CancellationToken ct = default);

    /// <summary>
    /// Install a single package specified as `name` or `name@version` into the project.
    /// Returns true on success.
    /// </summary>
    Task<bool> InstallPackageAsync(string projectPath, string packageSpec, bool saveCategories = true, CancellationToken ct = default);
}

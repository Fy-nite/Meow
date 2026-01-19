using System.Threading;
using System.Threading.Tasks;

namespace Meow.Core.Services;

public interface IInstallService
{
    Task<bool> InstallAsync(string projectPath, bool saveCategories = true, CancellationToken ct = default);
    Task<bool> UpdateAsync(string projectPath, CancellationToken ct = default);
}

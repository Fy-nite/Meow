using Meow.CLI.Commands;
using Microsoft.Extensions.DependencyInjection;
using Meow.Core.Services;

namespace Meow.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
            try
            {
                // Register services with DI container
                var services = new ServiceCollection();
                services.AddSingleton<IConfigService, ConfigService>();
                services.AddTransient<IProjectService, ProjectService>();
                // Register BuildService as concrete so CommandHandler can use CreateCompiler and Debug helpers
                services.AddTransient<BuildService>();
                services.AddTransient<IBuildService, BuildService>();

                // Configure HttpClient-backed PurrNet client
                services.AddHttpClient<IPurrNetService, PurrNetService>(c =>
                {
                    c.BaseAddress = new Uri("https://purr.finite.ovh/api/v1/");
                });

                services.AddTransient<IInstallService, InstallService>();
                services.AddTransient<CommandHandler>();

                var provider = services.BuildServiceProvider();

                var commandHandler = provider.GetRequiredService<CommandHandler>();
                return await commandHandler.HandleCommandAsync(args);
            }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }
}

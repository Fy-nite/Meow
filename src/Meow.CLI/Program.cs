using Meow.CLI.Commands;

namespace Meow.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
            try
            {
                // If you later add DI to the CLI, register the PurrNet client like this:
                // var services = new ServiceCollection();
                // services.AddHttpClient<Meow.Core.Services.IPurrNetService, Meow.Core.Services.PurrNetService>(c =>
                // {
                //     c.BaseAddress = new Uri("https://purrnet.example/api/v1/");
                // });
                // var provider = services.BuildServiceProvider();
                // var purr = provider.GetRequiredService<Meow.Core.Services.IPurrNetService>();

                var commandHandler = new CommandHandler();
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

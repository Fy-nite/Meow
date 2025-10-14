using Meow.CLI.Commands;

namespace Meow.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
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

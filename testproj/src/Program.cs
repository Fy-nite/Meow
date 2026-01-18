using System;
namespace Cat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello world");
            ShellRun();
        }
        public static void ShellRun()
        {
            while (true){
            string cmd = Console.ReadLine();
                switch (cmd)
                {
                    case "exit":
                        return;
                    case "hello":
                        Console.WriteLine("Hello user!");
                        break;
                    default:
                        Console.WriteLine($"Unknown command: {cmd}");
                        break;
                }
            }
            
        }
    }
}
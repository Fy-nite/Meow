using System;
namespace Meow.Core.Services
{
    using System.Collections.Concurrent;
    using System.Diagnostics;
    public class ConsoleProgressReporter : IProgressReporter
    {
        private readonly ConcurrentDictionary<string, Stopwatch> _fileTimers = new();

        public void StartFile(string currentFile)
        {
            var sw = new Stopwatch();
            sw.Start();
            _fileTimers[currentFile] = sw;
        }

        public void EndFile(string currentFile, TimeSpan elapsed)
        {
            Console.WriteLine($"Finished: {currentFile} in {elapsed.TotalSeconds:0.00}s");
            _fileTimers.TryRemove(currentFile, out _);
        }

        public void Report(string currentFile, double percent, TimeSpan? elapsed = null)
        {
            var elapsedStr = elapsed.HasValue ? $" | Elapsed: {elapsed.Value.TotalSeconds:0.00}s" : string.Empty;
            Console.WriteLine($"Compiling: {currentFile} ({percent:0.##}%)" + elapsedStr);
        }
    }
}
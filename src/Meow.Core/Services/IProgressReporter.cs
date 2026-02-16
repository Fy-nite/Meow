namespace Meow.Core.Services
{
    public interface IProgressReporter
    {
        void Report(string currentFile, double percent, TimeSpan? elapsed = null);
        void StartFile(string currentFile);
        void EndFile(string currentFile, TimeSpan elapsed);
    }
}
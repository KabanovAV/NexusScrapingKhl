using Serilog;
using System.Diagnostics;

namespace ScrapingKhl.Services
{
    public class TimeMeasurer : IDisposable
    {
        private readonly Stopwatch _timer = new();
        private readonly string? _operationName;

        public TimeMeasurer(string operationName)
        {
            _operationName = operationName;
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Stop();
            Log.Information($"{_operationName} {(int)_timer.Elapsed.TotalMilliseconds} мс");
        }
    }
}

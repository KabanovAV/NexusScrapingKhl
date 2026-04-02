using Serilog;

namespace ScrapingKhl.Services
{
    public interface IRequestThrottler
    {
        Task DelayAsync();
    }

    public class RequestThrottler : IRequestThrottler
    {
        private readonly int _minDelay;
        private readonly int _maxJitter;

        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public RequestThrottler(int minDelay = 1000, int maxJitter = 2000)
        {
            _minDelay = minDelay;
            _maxJitter = maxJitter;
        }

        public async Task DelayAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var elapsed = DateTime.UtcNow - _lastRequestTime;
                var remaining = _minDelay - (int)elapsed.TotalMilliseconds;

                if (remaining > 0)
                {
                    var jitter = Random.Shared.Next(_maxJitter);
                    var delay = remaining + jitter;
                    Log.Debug($"Задержка {delay} мс");
                    await Task.Delay(delay);
                }
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}

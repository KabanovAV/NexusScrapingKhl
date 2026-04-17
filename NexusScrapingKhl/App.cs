using CsvHelper;
using ScrapingKhl.Services;
using Serilog;
using System.Globalization;
using System.Text;

namespace ScrapingKhl
{
    public class App
    {
        private readonly IRequestThrottler _requestThrottler;
        private readonly IScrapingClubService _scrapingClubService;

        public App(IRequestThrottler requestThrottler, IScrapingClubService scrapingClubService)
        {
            _requestThrottler = requestThrottler;
            _scrapingClubService = scrapingClubService;
        }

        public async Task Run()
        {
            var clubs = new List<Club>();

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var clubCards = await _scrapingClubService.ScrapingClubCardAsync("clubs", cts.Token);
            if (clubCards == null || clubCards.Count == 0)
            {
                Log.Error("Список команд пустой! Программа отработана и завершает работу");
                return;
            }
            var clubUrls = clubCards.Select(c => c.SelectSingleNode(".//a[contains(concat(' ', normalize-space(@class), ' '), ' club-vertical ')]")
                .GetAttributeValue("href", string.Empty)).ToList();

            var semaphore = new SemaphoreSlim(5);
            var tasks = clubUrls.Select(async url =>
            {
                await semaphore.WaitAsync();
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                    await _requestThrottler.DelayAsync();
                    return await _scrapingClubService.ScrapingClubAsync(url, cts.Token);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            clubs = [.. results.Where(c => c != null)];

            using (var writer = new StreamWriter("clubs.csv", false, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(clubs);
            }
            Console.WriteLine("Export complete! Check clubs.csv");

            Log.Information($"Программа успешно отработана и завершает работу");
        }
    }
}

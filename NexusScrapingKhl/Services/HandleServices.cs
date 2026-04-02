using HtmlAgilityPack;
using Serilog;

namespace ScrapingKhl.Services
{
    public static class HandleServices
    {
        private static readonly Random _rnd = new();
        private static DateTime _lastRequestTime = DateTime.MinValue;

        public static async Task<HtmlDocument?> LoadHtmlDocument(HttpClient client, Uri url)
        {
            using (new TimeMeasurer("Время выполнения загрузки страницы"))
            {
                HtmlDocument document = new HtmlDocument();
                var token = new CancellationTokenSource();
                token.CancelAfter(TimeSpan.FromSeconds(10));
                try
                {
                    Log.Information($"Загрузка страницы {url} ...");
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token.Token);
                    response.EnsureSuccessStatusCode();
                    await using var html = await response.Content.ReadAsStreamAsync();
                    document.Load(html);
                    Log.Information("Страница успешно загружена");
                    return document;
                }
                catch (TaskCanceledException ex) when (token.IsCancellationRequested)
                {
                    Log.Error($"Загрузка прервана по таймауту ({url}): {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Log.Error($"Ошибка загрузки ({url}): {ex.Message}");
                    return null;
                }
            }
        }

        public static async Task<byte[]> DownloadFileWithHttpClientAsync(HttpClient client, string url)
        {
            using (new TimeMeasurer("Время выполнения загрузки файла"))
            {
                var token = new CancellationTokenSource();
                token.CancelAfter(TimeSpan.FromSeconds(10));
                try
                {
                    //if (!url.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
                    //    url = url.Insert(0, "https:");

                    if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri)){
                        if(!uri.IsAbsoluteUri)
                            url = new Uri(client.BaseAddress, url).ToString();
                    }

                    using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token.Token))
                    {
                        response.EnsureSuccessStatusCode();
                        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            var memoryStream = new MemoryStream();
                            await contentStream.CopyToAsync(memoryStream);
                            return memoryStream.ToArray();
                        }
                    }
                }
                catch (TaskCanceledException ex) when (token.IsCancellationRequested)
                {
                    Log.Error($"Загрузка прервана по таймауту ({url}): {ex.Message}");
                    return Array.Empty<byte>();
                }
                catch (Exception ex)
                {
                    Log.Error($"Ошибка загрузки файла ({url}): {ex.Message}");
                    return Array.Empty<byte>();
                }
            }
        }

        public static void SaveByteArrayToFileWithStaticMethod(byte[] data, string filePath)
            => File.WriteAllBytes(filePath, data);

        public static async Task RespectfulDelay(int minDelay = 1000, int maxJitter = 2000)
        {
            var elapsed = DateTime.UtcNow - _lastRequestTime;
            var remainingDelay = minDelay - (int)elapsed.TotalMilliseconds;

            if (remainingDelay > 0)
            {
                var jitter = _rnd.Next(maxJitter);
                var totalDelay = remainingDelay + jitter;
                Log.Debug($"Задержка {totalDelay} мс");
                await Task.Delay(totalDelay);
            }
            _lastRequestTime = DateTime.UtcNow;
        }
    }
}

using Serilog;

namespace ScrapingKhl.Services
{
    public interface IHttpService
    {
        Task<Stream?> GetStreamAsync(string url, CancellationToken ct);
        Task<byte[]> GetBytesAsync(string url, CancellationToken ct);
    }

    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;

        public HttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Stream?> GetStreamAsync(string url, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync(BuildUri(url), HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync(ct);
            }
            catch (TaskCanceledException ex) when (ct.IsCancellationRequested)
            {
                Log.Error($"Загрузка прервана по таймауту ({BuildUri(url)}): {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка загрузки ({BuildUri(url)}): {ex.Message}");
                return null;
            }
        }

        public async Task<byte[]> GetBytesAsync(string url, CancellationToken ct)
        {
            try
            {
                var stream = await GetStreamAsync(url, ct);
                if (stream == null)
                    return [];

                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                return ms.ToArray();
            }
            catch (TaskCanceledException ex) when (ct.IsCancellationRequested)
            {
                Log.Error($"Загрузка прервана по таймауту ({BuildUri(url)}): {ex.Message}");
                return [];
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка загрузки файла ({BuildUri(url)}): {ex.Message}");
                return [];
            }
        }

        private Uri BuildUri(string url)
        {
            //if (Uri.TryCreate(url, UriKind.Absolute, out var abs))
            //    return abs;
            return new Uri(_httpClient.BaseAddress!, url);
        }
    }
}

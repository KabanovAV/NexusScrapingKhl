using HtmlAgilityPack;
using Serilog;

namespace ScrapingKhl.Services
{
    public interface IHtmlLoader
    {
        Task<HtmlDocument?> LoadAsync(string url, CancellationToken ct);
    }

    public class HtmlLoader : IHtmlLoader
    {
        private readonly IHttpService _httpService;

        public HtmlLoader(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<HtmlDocument?> LoadAsync(string url, CancellationToken ct)
        {
            using (new TimeMeasurer("Время выполнения загрузки страницы"))
            {
                Log.Information($"Загрузка страницы {url} ...");
                var htmlDocument = new HtmlDocument();
                var stream = await _httpService.GetStreamAsync(url, ct);
                if (stream == null)
                    return null;

                htmlDocument.Load(stream);
                Log.Information("Страница успешно загружена");
                return htmlDocument;
            }
        }
    }
}

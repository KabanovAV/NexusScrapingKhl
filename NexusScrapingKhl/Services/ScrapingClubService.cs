using HtmlAgilityPack;

namespace ScrapingKhl.Services
{
    public interface IScrapingClubService
    {
        Task<HtmlNodeCollection?> ScrapingClubCardAsync(string clubsUrl, CancellationToken ct);
        Task<Club?> ScrapingClubAsync(string clubUrl, CancellationToken ct);
    }

    public class ScrapingClubService : IScrapingClubService
    {
        private readonly IHttpService _httpService;
        private readonly IHtmlLoader _httpLoader;
        

        public ScrapingClubService(IHttpService httpService, IHtmlLoader httpLoader)
        {
            _httpService = httpService;
            _httpLoader = httpLoader;
        }

        public async Task<HtmlNodeCollection?> ScrapingClubCardAsync(string clubsUrl, CancellationToken ct)
        {
            var htmlDocument = await _httpLoader.LoadAsync(clubsUrl, ct);
            if (htmlDocument == null) return null;
            return htmlDocument.DocumentNode.SelectNodes("//div[contains(concat(' ', normalize-space(@class), ' '), ' cardClub-card ')]");
        }

        public async Task<Club?> ScrapingClubAsync(string clubUrl, CancellationToken ct)
        {
            var htmlDocument = await _httpLoader.LoadAsync(clubUrl, ct);
            if (htmlDocument == null)
                return null;

            var clubName = htmlDocument.DocumentNode.SelectSingleNode(".//p[contains(concat(' ', normalize-space(@class), ' '), ' infoclub-club__info-name ')]").InnerText.Trim();
            var clubLocation = htmlDocument.DocumentNode.SelectSingleNode(".//p[contains(concat(' ', normalize-space(@class), ' '), ' infoclub-club__info-local ')]").InnerText.Trim();

            var clubImageUrl = htmlDocument.DocumentNode.SelectSingleNode(".//img[contains(concat(' ', normalize-space(@class), ' '), ' infoclub-club__logo-img ')]")
                .GetAttributeValue("src", string.Empty);
            var clubImage = await _httpService.GetBytesAsync(clubImageUrl, ct);

            var clubArenaLink = htmlDocument.DocumentNode.SelectNodes(".//a[contains(concat(' ', normalize-space(@class), ' '), ' filter-smallCheckbox__item ')]")
                .Last().GetAttributeValue("href", string.Empty);

            htmlDocument = await _httpLoader.LoadAsync(clubArenaLink, ct);
            if (htmlDocument == null) return null;

            var clubArena = HtmlEntity.DeEntitize(htmlDocument!.DocumentNode.SelectSingleNode(".//h2[contains(@class,'arena-info__title')]").InnerText.Trim());
            return new() { Name = clubName, Location = clubLocation, Arena = clubArena, Image = clubImage };
        }        
    }
}

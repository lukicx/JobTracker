using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using JobTracker.DAL.Models;

namespace JobTracker.DAL.Providers;

public class JobsCzJobProvider
{
    private readonly HttpClient _httpClient;

    public JobsCzJobProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(List<ExternalJob> Jobs, bool HasMore)> SearchJobsAsync(
        string keywords, string location, int page = 1)
    {
        // Jobs.cz city URLs use lowercase names without Czech diacritics.
        var city = new string(location.Trim().Normalize(NormalizationForm.FormD)
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .ToArray()).ToLowerInvariant();
        city = Regex.Replace(city, @"\s+", "-");

        var path = city.Length == 0 ? "/prace/" : $"/prace/{Uri.EscapeDataString(city)}/";
        var searchUrl = $"https://www.jobs.cz{path}?q={Uri.EscapeDataString(keywords.Trim())}&page={page}";
        if (city.Length > 0)
        {
            searchUrl += "&locality%5Bradius%5D=0";
        }

        using var response = await _httpClient.GetAsync(searchUrl);
        response.EnsureSuccessStatusCode();

        var document = new HtmlDocument();
        document.LoadHtml(await response.Content.ReadAsStringAsync());

        if (document.DocumentNode.SelectSingleNode("//*[@data-testid='zero-search-results-message']") is not null)
        {
            return ([], false);
        }

        var results = document.DocumentNode.SelectSingleNode(
            "//*[@id='search-result-container' or @data-testid='search-results-section-stack']");
        if (results is null)
        {
            throw new InvalidOperationException("Jobs.cz did not return a recognized search results page.");
        }

        var cards = results.SelectNodes(".//article[.//a[@data-jobad-id or @data-testid='advert-link']]");
        if (cards is null)
        {
            if (ReadText(results).Contains("Nenašli jsme žádné nabídky práce odpovídající zadání"))
            {
                return ([], false);
            }

            throw new InvalidOperationException("Jobs.cz search results no longer contain recognized job cards.");
        }

        var baseUri = response.RequestMessage!.RequestUri!;
        var jobs = new List<ExternalJob>();
        foreach (var card in cards)
        {
            var link = card.SelectSingleNode(".//a[@data-jobad-id or @data-testid='advert-link']")!;
            var id = link.GetAttributeValue("data-jobad-id", "");
            if (id.Length == 0)
            {
                id = card.GetAttributeValue("id", "").Replace("advert-", "");
            }

            var company = card.SelectSingleNode(".//footer//li[1]//span[@translate='no']")
                ?? card.SelectSingleNode(".//li[span[contains(., 'Název firmy')]]/span[2]");
            var locality = card.SelectSingleNode(".//*[@data-test='serp-locality']")
                ?? card.SelectSingleNode(".//li[span[contains(., 'Lokalita')]]/span[2]");
            var highlights = card.SelectNodes(".//span[contains(concat(' ', normalize-space(@class), ' '), ' Tag ')]");

            var title = ReadText(link);
            var companyName = ReadText(company);
            if (id.Length == 0 || title.Length == 0 || companyName.Length == 0
                || !Uri.TryCreate(baseUri, HtmlEntity.DeEntitize(link.GetAttributeValue("href", "")), out var jobUri)
                || jobUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Jobs.cz returned a job card with missing or unexpected fields.");
            }

            jobs.Add(new ExternalJob
            {
                ExternalId = $"jobscz:{id}",
                Company = companyName,
                Position = title,
                Location = ReadText(locality),
                Url = jobUri.GetLeftPart(UriPartial.Path),
                Description = highlights is null ? null : string.Join(" · ", highlights.Select(ReadText))
            });
        }

        var nextLink = document.DocumentNode.SelectSingleNode(
            "//a[@rel='next' or contains(@class, 'Pagination__button--next')]");
        return (jobs.DistinctBy(job => job.ExternalId).ToList(), nextLink is not null);
    }

    private static string ReadText(HtmlNode? node)
    {
        return Regex.Replace(HtmlEntity.DeEntitize(node?.InnerText ?? ""), @"\s+", " ").Trim();
    }
}

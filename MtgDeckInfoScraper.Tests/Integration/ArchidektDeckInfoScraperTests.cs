using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Moq;
using MtgDeckInfoScraper.Scraping.Archidekt;
using MtgDeckInfoScraper.Scraping.Models;
using MtgDeckInfoScraper.Scraping.Options;

namespace MtgDeckInfoScraper.Tests.Integration;

public class ArchidektDeckInfoScraperTests
{
    /// <summary>
    /// Test double: bypasses the real browser by overriding WithBrowserContextAsync,
    /// ScrapeDeckLinks, and NavigateToAndScrapeDeckInfoAsync.
    /// </summary>
    private sealed class TestArchidektScraper(
        ILogger<ArchidektDeckInfoScraper> logger,
        IMtgDeckInfoScraperOptions options,
        List<string> links,
        Func<string, DeckInfo> deckFactory)
        : ArchidektDeckInfoScraper(logger, options)
    {
        protected override Task<T> WithBrowserContextAsync<T>(Func<IBrowserContext, Task<T>> action)
            => action(Mock.Of<IBrowserContext>());

        protected override Task<List<string>> ScrapeDeckLinks(IBrowserContext existingBrowserContext, string deckListUrl)
            => Task.FromResult(links);

        protected override Task<DeckInfo> NavigateToAndScrapeDeckInfoAsync(IBrowserContext browserContext, string link)
            => Task.FromResult(deckFactory(link));
    }

    private static IMtgDeckInfoScraperOptions CreateOptions() =>
        Mock.Of<IMtgDeckInfoScraperOptions>(o => o.ArchidektMaxConcurrentScrapes == 5);

    [Fact]
    public async Task ScrapeDeckList_NullMaxCost_ReturnsConcurrentResultsUpToCount()
    {
        var links = Enumerable.Range(0, 5).Select(i => $"https://archidekt.com/decks/{i}").ToList();
        var scraper = new TestArchidektScraper(
            Mock.Of<ILogger<ArchidektDeckInfoScraper>>(),
            CreateOptions(),
            links,
            link => new DeckInfo("Deck", link, '$', 50.0, "2025-01-01", 10, 100));

        var result = await scraper.ScrapeDeckList("https://archidekt.com/decks", count: 3);

        Assert.Equal(3, result.Count);
        Assert.All(result, d => Assert.NotNull(d.Url));
    }

    [Fact]
    public async Task ScrapeDeckList_WithMaxCost_FiltersExpensiveDecksAndReturnsOnlyAffordable()
    {
        // deck0=$50, deck1=$200, deck2=$80, deck3=$300, deck4=$40
        var prices = new[] { 50.0, 200.0, 80.0, 300.0, 40.0 };
        var links = Enumerable.Range(0, 5).Select(i => $"https://archidekt.com/decks/{i}").ToList();
        var scraper = new TestArchidektScraper(
            Mock.Of<ILogger<ArchidektDeckInfoScraper>>(),
            CreateOptions(),
            links,
            link =>
            {
                var idx = int.Parse(link[(link.LastIndexOf('/') + 1)..]);
                return new DeckInfo("Deck", link, '$', prices[idx], "2025-01-01", 10, 100);
            });

        var result = await scraper.ScrapeDeckList("https://archidekt.com/decks", count: 10, maxCost: 100.0);

        // deck0 (50), deck2 (80), deck4 (40) are <= 100
        Assert.Equal(3, result.Count);
        Assert.All(result, d => Assert.True(d.Price <= 100.0));
    }
}

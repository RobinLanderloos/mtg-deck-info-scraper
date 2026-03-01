using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Moq;
using MtgDeckInfoScraper.Scraping.Base;
using MtgDeckInfoScraper.Scraping.Models;
using MtgDeckInfoScraper.Scraping.Options;

namespace MtgDeckInfoScraper.Tests.Unit;

public class BaseDeckInfoScraperTests
{
    /// <summary>
    /// Minimal concrete subclass that lets tests control NavigateToAndScrapeDeckInfoAsync
    /// and exposes the protected helpers for direct invocation.
    /// </summary>
    private sealed class TestScraper(
        ILogger logger,
        IMtgDeckInfoScraperOptions options,
        int maxConcurrent,
        Func<string, DeckInfo> scrapeFactory)
        : BaseDeckInfoScraper(logger, options, maxConcurrent)
    {
        public override Task<List<DeckInfo>> ScrapeDeckList(string deckListUrl, int count = 10, double? maxCost = null)
            => throw new NotImplementedException();

        protected override Task<DeckInfo> NavigateToAndScrapeDeckInfoAsync(IBrowserContext browserContext, string link)
            => Task.FromResult(scrapeFactory(link));

        public Task<List<DeckInfo>> CallScrapeDeckListWithMaxCostAsync(
            IBrowserContext ctx, IReadOnlyList<string> links, int count, double maxCost)
            => ScrapeDeckListWithMaxCostAsync(ctx, links, count, maxCost);

        public Task<T> CallScrapeWithSemaphoreAsync<T>(Func<Task<T>> action)
            => ScrapeWithSemaphoreAsync(action);

        public int SemaphoreCurrentCount => ScrapeSemaphore.CurrentCount;
    }

    [Fact]
    public async Task ScrapeDeckListWithMaxCostAsync_FiltersDecksOverMaxCost()
    {
        // link1 = $50 (passes), link2 = $200 (filtered), link3 = $80 (passes)
        var scraper = MakeScraper(link => link switch
        {
            "link1" => new DeckInfo("A", link, '$', 50.0, "today", 0, 0),
            "link2" => new DeckInfo("B", link, '$', 200.0, "today", 0, 0),
            _ => new DeckInfo("C", link, '$', 80.0, "today", 0, 0),
        });

        var ctx = Mock.Of<IBrowserContext>();
        var links = new[] { "link1", "link2", "link3" };

        var result = await scraper.CallScrapeDeckListWithMaxCostAsync(ctx, links, count: 10, maxCost: 100.0);

        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.True(d.Price <= 100.0));
    }

    [Fact]
    public async Task ScrapeDeckListWithMaxCostAsync_StopsEarlyWhenCountReached()
    {
        var scraper = MakeScraper(link => new DeckInfo("D", link, '$', 50.0, "today", 0, 0));
        var ctx = Mock.Of<IBrowserContext>();
        var links = Enumerable.Range(1, 10).Select(i => $"link{i}").ToArray();

        var result = await scraper.CallScrapeDeckListWithMaxCostAsync(ctx, links, count: 3, maxCost: 100.0);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task ScrapeWithSemaphoreAsync_ReleasesOnException()
    {
        var scraper = MakeScraper(_ => new DeckInfo("E", "u", '$', 1.0, "today", 0, 0), maxConcurrent: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scraper.CallScrapeWithSemaphoreAsync<int>(
                () => Task.FromException<int>(new InvalidOperationException("boom"))));

        Assert.Equal(1, scraper.SemaphoreCurrentCount);
    }

    private static TestScraper MakeScraper(Func<string, DeckInfo> factory, int maxConcurrent = 5)
    {
        var logger = Mock.Of<ILogger<BaseDeckInfoScraperTests>>();
        var options = Mock.Of<IMtgDeckInfoScraperOptions>(o =>
            o.MoxfieldMaxConcurrentScrapes == 5 &&
            o.ArchidektMaxConcurrentScrapes == 5);
        return new TestScraper(logger, options, maxConcurrent, factory);
    }
}

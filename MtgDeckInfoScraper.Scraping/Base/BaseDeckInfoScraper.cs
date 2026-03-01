using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MtgDeckInfoScraper.Scraping.Abstractions;
using MtgDeckInfoScraper.Scraping.Models;
using MtgDeckInfoScraper.Scraping.Options;

namespace MtgDeckInfoScraper.Scraping.Base;

public abstract class BaseDeckInfoScraper : IDeckInfoScraper
{
	protected readonly ILogger Logger;
	protected readonly IMtgDeckInfoScraperOptions Options;
	protected readonly SemaphoreSlim ScrapeSemaphore;

	protected BaseDeckInfoScraper(ILogger logger, IMtgDeckInfoScraperOptions options, int maxConcurrentScrapes)
	{
		Logger = logger;
		Options = options;
		ScrapeSemaphore = new SemaphoreSlim(maxConcurrentScrapes, maxConcurrentScrapes);
	}

	public abstract Task<List<DeckInfo>> ScrapeDeckList(string deckListUrl, int count = 10, double? maxCost = null);

	protected virtual async Task<T> WithBrowserContextAsync<T>(Func<IBrowserContext, Task<T>> action)
	{
		using var playwright = await Playwright.CreateAsync();
		Logger.LogDebug("Playwright instance created.");

		await using var browser = await playwright.Chromium.ConnectOverCDPAsync(
			$"http://localhost:{Options.ChromeRemoteDebuggingPort}", new()
			{
				SlowMo = 1000,
			});

		return await action(browser.Contexts[0]);
	}

	protected async Task<T> ScrapeWithSemaphoreAsync<T>(Func<Task<T>> action)
	{
		await ScrapeSemaphore.WaitAsync();
		try
		{
			return await action();
		}
		finally
		{
			ScrapeSemaphore.Release();
		}
	}

	protected async Task<List<DeckInfo>> ScrapeDeckListWithMaxCostAsync(
		IBrowserContext browserContext,
		IReadOnlyList<string> deckLinks,
		int count,
		double maxCost)
	{
		var results = new List<DeckInfo>();
		foreach (var link in deckLinks)
		{
			if (results.Count >= count) break;

			DeckInfo? info = null;
			try
			{
				info = await ScrapeWithSemaphoreAsync(() => NavigateToAndScrapeDeckInfoAsync(browserContext, link));
			}
			catch (Exception)
			{
				// skip failed scrapes
			}

			if (info is not null && info.Price <= maxCost)
				results.Add(info);
		}

		Logger.LogInformation("Completed scraping with maxCost filter. Total decks scraped: {TotalDecks}", results.Count);
		return results;
	}

	protected abstract Task<DeckInfo> NavigateToAndScrapeDeckInfoAsync(IBrowserContext browserContext, string link);
}

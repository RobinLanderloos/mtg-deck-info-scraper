using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MtgDeckInfoScraper.Scraping.Abstractions;
using MtgDeckInfoScraper.Scraping.Models;
using MtgDeckInfoScraper.Scraping.Options;

namespace MtgDeckInfoScraper.Scraping.Moxfield;

public class MoxfieldDeckInfoScraper : IDeckInfoScraper
{
	private readonly ILogger<MoxfieldDeckInfoScraper> _logger;
	private readonly IMtgDeckInfoScraperOptions _options;
	private readonly SemaphoreSlim _scrapeSemaphore;

	public MoxfieldDeckInfoScraper(ILogger<MoxfieldDeckInfoScraper> logger,
		IMtgDeckInfoScraperOptions options)
	{
		_logger = logger;
		_options = options;
		_scrapeSemaphore = new SemaphoreSlim(options.MoxfieldMaxConcurrentScrapes, options.MoxfieldMaxConcurrentScrapes);
	}


	public async Task<List<DeckInfo>> ScrapeDeckList(string deckListUrl,
		int count = 10,
		double? maxCost = null)
	{
		using var playwright = await Playwright.CreateAsync();
		_logger.LogDebug("Playwright instance created.");

		await using var browser = await playwright.Chromium.ConnectOverCDPAsync(
			$"http://localhost:{_options.ChromeRemoteDebuggingPort}", new()
			{
				SlowMo = 1000,
			});

		_logger.LogInformation("Starting to scrape deck list from URL: {DeckListUrl} with count: {Count}", deckListUrl,
			count);

		var existingBrowserContext = browser.Contexts[0];

		var deckLinks = await ScrapeDeckLinks(existingBrowserContext, deckListUrl);
		_logger.LogDebug("Retrieved {DeckLinkCount} deck links", deckLinks.Count);

		if (maxCost is null)
		{
			var toCheck = count > deckLinks.Count ? deckLinks : deckLinks.Take(count);

			var scrapeTasks = toCheck.Select(async link =>
			{
				await _scrapeSemaphore.WaitAsync();
				try
				{
					var info = await NavigateToAndScrapeDeckInfo(existingBrowserContext, link);
					return info;
				}
				finally
				{
					_scrapeSemaphore.Release();
				}
			});

			var infos = (await Task.WhenAll(scrapeTasks)).ToList();

			_logger.LogInformation("Completed scraping. Total decks scraped: {TotalDecks}", infos.Count);
			return infos;
		}
		else
		{
			var results = new List<DeckInfo>();
			foreach (var link in deckLinks)
			{
				if (results.Count >= count) break;

				await _scrapeSemaphore.WaitAsync();
				DeckInfo info;
				try
				{
					info = await NavigateToAndScrapeDeckInfo(existingBrowserContext, link);
				}
				finally
				{
					_scrapeSemaphore.Release();
				}

				if (info.Price <= maxCost)
					results.Add(info);
			}

			_logger.LogInformation("Completed scraping with maxCost filter. Total decks scraped: {TotalDecks}", results.Count);
			return results;
		}
	}


	private async Task<DeckInfo> NavigateToAndScrapeDeckInfo(IBrowserContext browserContext,
		string deckUrl)
	{
		_logger.LogInformation("Navigating to deck URL: {DeckUrl}", deckUrl);

		var page = await browserContext.NewPageAsync();
		_logger.LogInformation("Navigating to the URL: {DeckUrl}", deckUrl);
		await page.GotoAsync(deckUrl);

		await page.WaitForSelectorAsync("section[class='deckview']");

		var title = await ScrapeDeckTitle(page);

		var price = await ScrapeDeckPrice(page);

		var lastUpdated = await ScrapeLastUpdated(page);

		var likes = await ScrapeLikes(page);

		var views = await ScrapeViews(page);

		await page.CloseAsync();

		var deckInfo = new DeckInfo(title, deckUrl, price.Item1, price.Item2, lastUpdated, likes, views);
		_logger.LogDebug("Constructed DeckInfo object: {DeckInfo}", deckInfo);

		return deckInfo;
	}

	private async Task<List<string>> ScrapeDeckLinks(IBrowserContext browserContext,
		string deckListUrl)
	{
		_logger.LogInformation("Scraping deck links from URL: {DeckListUrl}", deckListUrl);

		var page = await browserContext.NewPageAsync();
		await page.GotoAsync(deckListUrl);

		await page.WaitForSelectorAsync("div[class='browse-ad-layout']");
		_logger.LogDebug("Waiting for deck list to load");

		var links = await page.QuerySelectorAllAsync("a[href*='/decks/']");
		var hrefs = await Task.WhenAll(links.Select(async link => await link.GetAttributeAsync("href")));

		var validLinks = hrefs
			.Where(link => link is not null && link.Contains("/decks/") &&
			               !link.Contains("/decks/public") &&
			               !link.Contains("/decks/liked") &&
			               !link.Contains("/decks/following") &&
			               !link.Contains("/decks/personal") &&
			               !link.Contains("/decks/private"))
			.Select(link => "https://www.moxfield.com" + link)
			.ToList();

		await page.CloseAsync();

		_logger.LogInformation("Retrieved {ValidLinkCount} valid deck links", validLinks.Count);
		return validLinks;
	}

	private async Task<string> ScrapeDeckTitle(IPage page) =>
		await SafeScrape(page, "span[class='deckheader-name']", "title", "No title found");

	private async Task<string> ScrapeLastUpdated(IPage page) =>
		await SafeScrape(page, "#lastupdated", "last updated", "No last updated found");

	private async Task<int> ScrapeViews(IPage page)
	{
		const string viewsXPath = "//*[@id='deck-header-views']/div[2]";
		var viewsText = await SafeScrape(page, viewsXPath, "views", "0");
		return int.Parse(viewsText, NumberStyles.AllowThousands, new CultureInfo("en-US"));
	}

	private async Task<int> ScrapeLikes(IPage page)
	{
		var likesText = await SafeScrape(page, "div[id='deck-header-like']", "likes", "0");
		return int.Parse(likesText, NumberStyles.AllowThousands, new CultureInfo("en-US"));
	}

	private async Task<string> SafeScrape(IPage page,
		string selector,
		string fieldName,
		string defaultValue)
	{
		try
		{
			var element = await page.QuerySelectorAsync(selector);

			if (element is null) return defaultValue;

			var text = await element.TextContentAsync();
			return text ?? defaultValue;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to scrape {FieldName}. Returning default value: {DefaultValue}", fieldName,
				defaultValue);
			return defaultValue;
		}
	}

	private async Task<(char, double)> ScrapeDeckPrice(IPage page)
	{
		_logger.LogInformation("Scraping deck price...");
		try
		{
			var priceElement = await page.QuerySelectorAsync("span[id='shoppingcart']");
			await priceElement!.HoverAsync();

			await page.WaitForSelectorAsync(".tooltip.show", new PageWaitForSelectorOptions()
			{
				State = WaitForSelectorState.Visible
			});

			var tooltipText = await page.EvalOnSelectorAsync<string>(".tooltip.show", @"el => el.innerText");

			var match = Regex.Match(tooltipText, @"With any printings:\s*([€$])([\d.,]+)");
			if (!match.Success)
			{
				return ('€', 0);
			}

			var symbol = match.Groups[1].Value[0];
			var priceString = match.Groups[2].Value.Replace(",", "");
			if (!double.TryParse(priceString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
				    out var price))
			{
				_logger.LogWarning("Failed to parse price string: {PriceString}. Returning default value of 0.", priceString);
				return (symbol, 0);
			}

			_logger.LogDebug("Scraped price: {Symbol}{Price}", symbol, price);

			return (symbol, price);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to scrape deck price. Returning default value of 0.");
			return ('$', 0);
		}
	}
}
using System.Globalization;
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

	public MoxfieldDeckInfoScraper(ILogger<MoxfieldDeckInfoScraper> logger,
		IMtgDeckInfoScraperOptions options)
	{
		_logger = logger;
		_options = options;
	}


	public async Task<List<DeckInfo>> ScrapeDeckList(string deckListUrl,
		int count = 10)
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

		var toCheck = count > deckLinks.Count ? deckLinks : deckLinks.Take(count);
		var deckList = new List<DeckInfo>();

		var chunkCount = 0;
		var deckCount = 0;
		foreach (var deckLinkChunk in toCheck.Chunk(5))
		{
			chunkCount++;
			_logger.LogDebug("Processing chunk {ChunkNumber}", chunkCount);

			var scrapeTasks = new List<Task>();
			foreach (var deckLink in deckLinkChunk)
			{
				deckCount++;
				var currentDeckCount = deckCount;
				_logger.LogDebug("Starting scrape for deck {DeckNumber}: {DeckLink}", currentDeckCount, deckLink);

				scrapeTasks.Add(Task.Run(async () =>
				{
					var info = await NavigateToAndScrapeDeckInfo(existingBrowserContext, deckLink);
					_logger.LogDebug("Scraped deck info for {DeckNumber}: {DeckInfo}", currentDeckCount, info);
					lock (deckList)
					{
						deckList.Add(info);
					}
				}));
			}
			await Task.WhenAll(scrapeTasks);

			// Rate limit our requests a bit to avoid getting blocked
			await Task.Delay(2000);
		}

		_logger.LogInformation("Completed scraping. Total decks scraped: {TotalDecks}", deckList.Count);
		return deckList;
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
		_logger.LogDebug("Deck title scraped: {Title}", title);

		var price = await ScrapeDeckPrice(page);
		_logger.LogDebug("Deck price scraped: {Price}", price);

		var lastUpdated = await ScrapeLastUpdated(page);
		_logger.LogDebug("Last updated timestamp scraped: {LastUpdated}", lastUpdated);

		var likes = await ScrapeLikes(page);
		_logger.LogDebug("Deck likes scraped: {Likes}", likes);

		var views = await ScrapeViews(page);
		_logger.LogDebug("Deck views scraped: {Views}", views);

		await page.CloseAsync();

		var deckInfo = new DeckInfo(title, deckUrl, price.Item1, price.Item2, lastUpdated, likes, views);
		_logger.LogDebug("Constructed DeckInfo object: {DeckInfo}", deckInfo);

		return deckInfo;
	}

	private async Task<List<string>> ScrapeDeckLinks(IBrowserContext browserContext,
		string deckListUrl)
	{
		_logger.LogInformation("Scraping deck links from URL: {DeckListUrl}", deckListUrl);

		using var playwright = await Playwright.CreateAsync();

		var page = await browserContext.NewPageAsync();
		await page.GotoAsync(deckListUrl);

		await page.WaitForSelectorAsync("div[class='browse-ad-layout']");
		_logger.LogInformation("Waiting for deck list to load");

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
			// Ugly code, refactor later
			var priceText = await SafeScrape(page, "span[id='shoppingcart']", "price", "$0");
			char symbol = '$';
			double price = 0;

			if (priceText.Contains('$'))
			{
				var dollarIndex = priceText.IndexOf('$');
				var priceSubstring = priceText.Substring(dollarIndex + 1).Trim();
				price = Convert.ToDouble(priceSubstring, new CultureInfo("en-US"));
				symbol = '$';
			}
			else if (priceText.Contains("\u20ac")) // Euro symbol
			{
				var euroIndex = priceText.IndexOf('\u20ac');
				var priceSubstring = priceText.Substring(euroIndex + 1).Trim();
				price = Convert.ToDouble(priceSubstring, new CultureInfo("en-US"));
				symbol = '\u20ac';
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
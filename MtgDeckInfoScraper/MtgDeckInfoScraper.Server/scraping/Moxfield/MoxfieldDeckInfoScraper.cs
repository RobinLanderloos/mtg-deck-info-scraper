using System.Globalization;
using Microsoft.Playwright;

namespace MtgDeckInfoScraper.Server.scraping.Moxfield;

public class MoxfieldDeckInfoScraper
{
	private const bool Headless = false;

	public async Task<List<DeckInfo>> ScrapeDeckList(string deckListUrl,
		int count = 10)
	{
		var deckLinks = await ScrapeDeckLinks(deckListUrl);

		var toCheck = count > deckLinks.Count ? deckLinks : deckLinks.Take(count);

		var deckList = new List<DeckInfo>();

		int chunkCount = 0;
		int deckCount = 0;
		foreach (var deckLinkChunk in toCheck.Chunk(5))
		{
			chunkCount++;
			Console.WriteLine($"Scraping chunk {chunkCount}");
			var scrapeTasks = new List<Task>();
			foreach (var deckLink in deckLinkChunk)
			{
				scrapeTasks.Add(Task.Run(async () =>
				{
					deckCount++;
					Console.WriteLine($"Scraping deck {deckCount}: {deckLink}");
					var info = await NavigateToAndScrapeDeckInfo(deckLink);
					Console.WriteLine(info);
					deckList.Add(info);
				}));
			}

			await Task.WhenAll(scrapeTasks);
		}


		return deckList;
	}

	/// <summary>
	/// Scrape the deck info from the given url, expect
	/// </summary>
	/// <param name="deckUrl"></param>
	/// <returns></returns>
	private async Task<DeckInfo> NavigateToAndScrapeDeckInfo(string deckUrl)
	{
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new()
		{
			Headless = Headless,
		});

		var page = await browser.NewPageAsync();
		await page.GotoAsync(deckUrl);

		await page.WaitForSelectorAsync("article[class='deckview']");

		// Get title
		var title = await ScrapeDeckTitle(page);

		// Get price
		var price = await ScrapeDeckPrice(page);
		// Get last updated
		var lastUpdated = await ScrapeLastUpdated(page);
		// Get likes
		var likes = await ScrapeLikes(page);
		// Get views
		var views = await ScrapeViews(page);

		return new DeckInfo(title, deckUrl, price, lastUpdated, likes, views);
	}

	private static async Task<int> ScrapeViews(IPage page)
	{
		const string viewsXPath = "//*[@id='deck-header-views']/div[2]";

		var viewsText = await GetTextBySelector(page, viewsXPath);
		var views = int.Parse(viewsText ?? "0", NumberStyles.AllowThousands, new CultureInfo("en-US"));

		return views;
	}

	private static async Task<int> ScrapeLikes(IPage page)
	{
		var likesText = await GetTextBySelector(page, "div[id='deck-header-like']");

		var likes = int.Parse(likesText ?? "0", NumberStyles.AllowThousands, new CultureInfo("en-US"));
		return likes;
	}

	private static async Task<string?> GetTextBySelector(IPage page,
		string selector)
	{
		var element = await page.QuerySelectorAsync(selector);

		if (element == null)
		{
			return null;
		}

		return await element.TextContentAsync();
	}

	private static async Task<string> ScrapeLastUpdated(IPage page)
	{
		const string lastUpdatedId = "lastupdated";

		var lastUpdatedElementText = await GetById(page, lastUpdatedId);
		return lastUpdatedElementText ?? "No last updated found";
	}


	private static async Task<double> ScrapeDeckPrice(IPage page)
	{
		await page.Locator("a")
			.Filter(new() { HasText = "BuyDeck" })
			.ClickAsync();

		var buyNowLocator = page
			.GetByRole(AriaRole.Button, new() { Name = "Buy Now for" });

		await buyNowLocator.WaitForAsync();

		await page.Locator("label")
			.Filter(new() { HasText = "TCGplayer" })
			.ClickAsync();

		var buyNowForText = await buyNowLocator.TextContentAsync();

		if (buyNowForText == null)
		{
			return 0;
		}

		var dollarIndex = buyNowForText.IndexOf('$');
		var priceSubstring = buyNowForText.Substring(dollarIndex + 1).Trim();
		var price = Convert.ToDouble(priceSubstring, new CultureInfo("en-US"));
		return price;
	}


	private static async Task<string> ScrapeDeckTitle(IPage page)
	{
		var titleSpan = await page.QuerySelectorAsync("span[class='deckheader-name']");

		if (titleSpan == null)
		{
			return "No title found";
		}

		return await titleSpan.TextContentAsync() ?? "No title found";
	}

	/// <summary>
	/// Scrapes the deck links from the current page, and returns them as a list of strings.
	/// </summary>
	private async Task<List<string>> ScrapeDeckLinks(string deckListUrl)
	{
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new()
		{
			Headless = Headless,
		});

		var page = await browser.NewPageAsync();
		await page.GotoAsync(deckListUrl);

		await page.WaitForSelectorAsync("div[class='browse-ad-layout']");

		Console.WriteLine("Waiting for deck list to load");

		var links = await page.QuerySelectorAllAsync("a[href*='/decks/']");

		var hrefs = await Task.WhenAll(
			links
				.Select(async link => await link.GetAttributeAsync("href"))
		);

		var validLinks = new List<string>();

		const string moxfieldBaseUrl = "https://www.moxfield.com";

		foreach (var link in hrefs)
		{
			if (link == null) continue;

			if (!link.Contains("/decks/")) continue;

			if (link.Contains("/decks/public")
			    || link.Contains("/decks/liked")
			    || link.Contains("/decks/following"))

			{
				continue;
			}

			validLinks.Add(moxfieldBaseUrl + link);
		}

		return validLinks;
	}

	private static async Task<string?> GetById(IPage page,
		string id)
	{
		var element = await page.QuerySelectorAsync($"#{id}");

		if (element == null)
		{
			return null;
		}

		return await element.TextContentAsync();
	}
}
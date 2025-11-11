using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MtgDeckInfoScraper.Scraping.Abstractions;
using MtgDeckInfoScraper.Scraping.Models;
using MtgDeckInfoScraper.Scraping.Options;

namespace MtgDeckInfoScraper.Scraping.Archidekt;

public class ArchidektDeckInfoScraper : IDeckInfoScraper
{
	private readonly SemaphoreSlim _scrapeSemaphore;
	private readonly ILogger<ArchidektDeckInfoScraper> _logger;
	private readonly IMtgDeckInfoScraperOptions _options;

	public ArchidektDeckInfoScraper(ILogger<ArchidektDeckInfoScraper> logger,
		IMtgDeckInfoScraperOptions options)
	{
		_logger = logger;
		_options = options;
		_scrapeSemaphore = new SemaphoreSlim(options.ArchidektMaxConcurrentScrapes, options.ArchidektMaxConcurrentScrapes);
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

		_logger.LogInformation("Starting to scrape deck list from URL: {DeckListUrl} with count: {Count}", deckListUrl, count);

		var existingBrowserContext = browser.Contexts[0];

		var deckLinks = await ScrapeDeckLinks(existingBrowserContext, deckListUrl);

		var scrapeTasks = deckLinks.Take(count).Select(async link =>
		{
			await _scrapeSemaphore.WaitAsync();
			try
			{
				var info = await NavigateToAndScrapeDeckInfo(existingBrowserContext, link);
				return info;
			}
			catch (Exception)
			{
				return null;
			}
			finally
			{
				_scrapeSemaphore.Release();
			}
		});

		var infos = (await Task.WhenAll(scrapeTasks)).Where(x => x is not null).ToList();

		return infos!;
	}

	private async Task<DeckInfo> NavigateToAndScrapeDeckInfo(IBrowserContext existingBrowserContext,
		string link)
	{
		var page = await existingBrowserContext.NewPageAsync();

		try
		{
			await page.GotoAsync(link);
			await page.EvaluateAsync(@"() => {
					const el = document.querySelector('.cmpwrapper');
					if (el) el.remove();
				}");

			var title = await (await page.QuerySelectorAsync("[class^=\"deckHeader_deckName__\"]"))!.InnerTextAsync();

			await page.HoverAsync("[class^=\"deckPrice_\"]");
			const string tooltipSelector = ".ui.popup.visible";
			await page.WaitForSelectorAsync(tooltipSelector, new PageWaitForSelectorOptions()
			{
				State = WaitForSelectorState.Visible
			});
			var tooltipText = await page.EvalOnSelectorAsync<string>(tooltipSelector, @"el => el.innerText");
			var match = Regex.Match(tooltipText, @"Any printings:\s*([\d,.]+)\s([€$])");
			if (!match.Success)
			{
				_logger.LogWarning("Could not find price information in tooltip for deck: {DeckLink}", link);
				throw new InvalidOperationException($"Could not find price information in tooltip for deck: {link}");
			}

			var priceString = match.Groups[1].Value.Replace(",", "");
			var currencySymbol = match.Groups[2].Value[0];
			if (!double.TryParse(priceString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price))
			{
				_logger.LogWarning("Could not parse price value '{PriceString}' for deck: {DeckLink}", priceString, link);
				throw new InvalidOperationException($"Could not parse price value '{priceString}' for deck: {link}");
			}

			var updatedAt = await page.QuerySelectorAllAsync("div[class^=\"shareAndSocialControls_updatedAt__\"]");
			var updatedAtText = await updatedAt[1].InnerTextAsync();

			var bannerButtons = page.Locator("span[class^=\"bannerButton_button__\"]");
			var thumbsUpButton = bannerButtons.Filter(new LocatorFilterOptions()
			{
				Has = page.Locator("i.thumbs.up.outline.icon")
			});

			var thumbsUpCount = await (await thumbsUpButton.AllAsync())[1].Locator("span").InnerTextAsync();

			var viewsCountElements = await page.QuerySelectorAllAsync("div[class^=\"shareAndSocialControls_viewCount__\"]");
			var viewsCountText = await viewsCountElements[1].InnerTextAsync();

			// Parse stuff like "9.7k" into an integer
			// Handle "k" for thousands
			var viewsCount = 0;
			if (viewsCountText.EndsWith("k"))
			{
				var numberPart = viewsCountText[..^1];
				if (double.TryParse(numberPart, out var number))
				{
					viewsCount = (int)(number * 1000);
				}
			}
			else
			{
				if (int.TryParse(viewsCountText.Replace(",", ""), out var number))
				{
					viewsCount = number;
				}
			}

			return new DeckInfo(title, link, currencySymbol, price, updatedAtText, int.Parse(thumbsUpCount), viewsCount);
		}
		catch (Exception ex)
		{
			_logger.LogError("Error scraping deck info from link: {DeckLink}. Exception: {ExceptionMessage}", link, ex.Message);
			throw;
		}
		finally
		{
			await page.CloseAsync();
		}
	}

	private async Task<List<string>> ScrapeDeckLinks(IBrowserContext existingBrowserContext,
		string deckListUrl)
	{
		var page = await existingBrowserContext.NewPageAsync();
		await page.GotoAsync(deckListUrl);

		var links = await page.QuerySelectorAllAsync("[class^=\"deckLink_thumbnail__\"]");
		var hrefs = await Task.WhenAll(links.Select(async link => await link.GetAttributeAsync("href")));

		await page.CloseAsync();

		_logger.LogDebug("Scraped {DeckLinkCount} deck links", hrefs.Length);
		return hrefs.Where(href => !string.IsNullOrEmpty(href)).Select(href => "https://archidekt.com" + href).ToList();
	}
}
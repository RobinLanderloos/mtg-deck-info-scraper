using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MtgDeckInfoScraper.Scraping.Base;
using MtgDeckInfoScraper.Scraping.Models;
using MtgDeckInfoScraper.Scraping.Options;

namespace MtgDeckInfoScraper.Scraping.Archidekt;

public class ArchidektDeckInfoScraper : BaseDeckInfoScraper
{
	public ArchidektDeckInfoScraper(ILogger<ArchidektDeckInfoScraper> logger,
		IMtgDeckInfoScraperOptions options)
		: base(logger, options, options.ArchidektMaxConcurrentScrapes)
	{
	}

	public override async Task<List<DeckInfo>> ScrapeDeckList(string deckListUrl,
		int count = 10,
		double? maxCost = null)
	{
		return await WithBrowserContextAsync(async browserContext =>
		{
			Logger.LogInformation("Starting to scrape deck list from URL: {DeckListUrl} with count: {Count}", deckListUrl, count);

			var deckLinks = await ScrapeDeckLinks(browserContext, deckListUrl);

			if (maxCost is null)
			{
				var scrapeTasks = deckLinks.Take(count).Select(async link =>
				{
					try
					{
						return (DeckInfo?)await ScrapeWithSemaphoreAsync(() => NavigateToAndScrapeDeckInfoAsync(browserContext, link));
					}
					catch (Exception)
					{
						return null;
					}
				});

				var infos = (await Task.WhenAll(scrapeTasks)).Where(x => x is not null).Cast<DeckInfo>().ToList();

				Logger.LogInformation("Completed scraping. Total decks scraped: {TotalDecks}", infos.Count);
				return infos;
			}

			return await ScrapeDeckListWithMaxCostAsync(browserContext, deckLinks, count, maxCost.Value);
		});
	}

	protected override async Task<DeckInfo> NavigateToAndScrapeDeckInfoAsync(IBrowserContext browserContext,
		string link)
	{
		var page = await browserContext.NewPageAsync();

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
				Logger.LogWarning("Could not find price information in tooltip for deck: {DeckLink}", link);
				throw new InvalidOperationException($"Could not find price information in tooltip for deck: {link}");
			}

			var priceString = match.Groups[1].Value.Replace(",", "");
			var currencySymbol = match.Groups[2].Value[0];
			if (!double.TryParse(priceString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price))
			{
				Logger.LogWarning("Could not parse price value '{PriceString}' for deck: {DeckLink}", priceString, link);
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
			Logger.LogError("Error scraping deck info from link: {DeckLink}. Exception: {ExceptionMessage}", link, ex.Message);
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

		Logger.LogDebug("Scraped {DeckLinkCount} deck links", hrefs.Length);
		return hrefs.Where(href => !string.IsNullOrEmpty(href)).Select(href => "https://archidekt.com" + href).ToList();
	}
}
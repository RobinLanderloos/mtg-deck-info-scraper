using System.Globalization;
using MxofieldDeckListsScraper.scraping.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace MxofieldDeckListsScraper.scraping.Moxfield;

public class MoxfieldDeckInfoScraper
{
	private readonly ChromeDriver _driver;
	private readonly WebDriverWait _waiter;

	public MoxfieldDeckInfoScraper()
	{
		_driver = new ChromeDriver();
		_waiter = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
	}

	public async Task<List<DeckInfo>> ScrapeDeckList(string url,
		int count = 10)
	{
		await _driver.Navigate().GoToUrlAsync(url);

		await Task.Delay(2000);

		var deckLinks = (ScrapeDeckLinks(_driver)).ToArray();

		var toCheck = count > deckLinks.Count() ? deckLinks.Count() : count;

		var deckList = new List<DeckInfo>();

		for (var i = 0; i < toCheck; i++)
		{
			var deckLink = deckLinks[i];
			await _driver.Navigate().GoToUrlAsync(deckLink);

			// await Task.Delay(2000);

			Console.WriteLine($"Scraping deck {i + 1} of {toCheck}: {deckLink}");
			var info = ScrapeDeckInfo(deckLink);

			Console.WriteLine(info);
			deckList.Add(info);
		}

		_driver.Quit();

		return deckList;
	}

	private DeckInfo ScrapeDeckInfo(string url)
	{
		// Get title
		var title = ScrapeDeckTitle(_driver);
		// Get price
		var price = ScrapeDeckPrice(_driver);
		// Get last updated
		var lastUpdated = ScrapeLastUpdated(_driver);
		// Get likes
		var likes = ScrapeLikes(_driver);
		// Get views
		var views = ScrapeViews(_driver);

		return new DeckInfo(title, url, price, lastUpdated, likes, views);
	}

	private int ScrapeViews(IWebDriver driver)
	{
		const string viewsXPath = "//*[@id='deck-header-views']/div[2]";

		var viewsElement = driver.FindElement(By.XPath(viewsXPath));
		var views = int.Parse(viewsElement.Text, NumberStyles.AllowThousands, new CultureInfo("en-US"));
		return views;
	}

	private int ScrapeLikes(IWebDriver driver)
	{
		const string likesXPath = "//*[@id='deck-header-like']/a/div[1]";

		var likesElement = driver.FindElement(By.XPath(likesXPath));
		var likes = int.Parse(likesElement.Text, NumberStyles.AllowThousands, new CultureInfo("en-US"));
		return likes;
	}

	private string ScrapeLastUpdated(IWebDriver driver)
	{
		const string lastUpdatedId = "lastupdated";

		var lastUpdatedElement = driver.FindElement(By.Id(lastUpdatedId));
		return lastUpdatedElement.Text;
	}

	private float ScrapeDeckPrice(IWebDriver driver)
	{
		const string buyButtonXPath = "//a[span[text()='Buy']]";
		const string buyNowForSpanXPath = "//span[starts-with(text(), 'Buy Now for')]";
		const string tcgPlayerButtonXPath = "//div[contains(text(), 'TCGplayer')]";

		var priceSpans = driver.FindElements(By.XPath(buyButtonXPath));
		priceSpans[0].Click();

		_waiter.Until(d => d.FindElements(By.XPath(buyNowForSpanXPath)).Count > 0);

		var tcgPlayerButton = driver.FindElement(By.XPath(tcgPlayerButtonXPath));
		tcgPlayerButton.Click();

		var buyNowForSpans = driver.FindElements(By.XPath(buyNowForSpanXPath));

		var dollarIndex = buyNowForSpans[0].Text.IndexOf('$');
		var price = Convert.ToDouble(buyNowForSpans[0].Text.Substring(dollarIndex + 1).Trim(), new CultureInfo("en-US"));
		return (float)price;
	}

	private string ScrapeDeckTitle(IWebDriver driver)
	{
		const string titleXPath = "//span[contains(@class, 'deckheader-name')]";
		var titleSpans = driver.WaitAndFindElements(By.XPath(titleXPath), 10);

		// Get the text from the first span
		var title = titleSpans.ElementAt(0).Text;

		return title ?? "No title found";
	}

	/// <summary>
	/// Scrapes the deck links from the current page, and returns them as a list of strings.
	/// </summary>
	private IEnumerable<string> ScrapeDeckLinks(IWebDriver driver)
	{
		var links = driver.FindElements(By.TagName("a")).Select(a => a?.GetDomProperty("href")).ToList();

		foreach (var link in links)
		{
			if(link == null) continue;

			if (!link.Contains("/decks/")) continue;

			if (link.Contains("/decks/public")
			    || link.Contains("/decks/liked")
			    || link.Contains("/decks/following"))

			{
				continue;
			}

			yield return link;
		}
	}
}
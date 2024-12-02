using System.Globalization;
using MtgDeckInfoScraper.Server.scraping.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace MtgDeckInfoScraper.Server.scraping.Moxfield;

public class MoxfieldDeckInfoScraper
{
    public async Task<List<DeckInfo>> ScrapeDeckList(string deckListUrl,
        int count = 10)
    {
        var deckLinksDriver = new ChromeDriver();
        await deckLinksDriver.Navigate().GoToUrlAsync(deckListUrl);

        await Task.Delay(2000);

        var deckLinks = (ScrapeDeckLinks(deckLinksDriver)).ToArray();

        deckLinksDriver.Quit();

        var toCheck = count > deckLinks.Length ? deckLinks : deckLinks.Take(count);

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
                    var driver = new ChromeDriver();

                    deckCount++;
                    Console.WriteLine($"Scraping deck {deckCount}: {deckLink}");
                    var info = await NavigateToAndScrapeDeckInfo(driver, deckLink);
                    Console.WriteLine(info);
                    deckList.Add(info);

                    driver.Quit();
                }));
            }

            await Task.WhenAll(scrapeTasks);
        }


        return deckList;
    }

    /// <summary>
    /// Scrape the deck info from the given url, expect
    /// </summary>
    /// <param name="driver"></param>
    /// <param name="deckUrl"></param>
    /// <returns></returns>
    private async Task<DeckInfo> NavigateToAndScrapeDeckInfo(IWebDriver driver, string deckUrl)
    {
        await driver.Navigate().GoToUrlAsync(deckUrl);
        await Task.Delay(2000);

        // Get title
        var title = ScrapeDeckTitle(driver);
        // Get price
        var price = ScrapeDeckPrice(driver);
        // Get last updated
        var lastUpdated = ScrapeLastUpdated(driver);
        // Get likes
        var likes = ScrapeLikes(driver);
        // Get views
        var views = ScrapeViews(driver);

        return new DeckInfo(title, deckUrl, price, lastUpdated, likes, views);
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

    private double ScrapeDeckPrice(IWebDriver driver)
    {
        const string buyButtonXPath = "//a[span[text()='Buy']]";
        const string buyNowForSpanXPath = "//span[starts-with(text(), 'Buy Now for')]";
        const string tcgPlayerButtonXPath = "//div[contains(text(), 'TCGplayer')]";

        var priceSpans = driver.FindElements(By.XPath(buyButtonXPath));
        var js = (IJavaScriptExecutor)driver;
        js.ExecuteScript("arguments[0].click();", priceSpans[0]);

        driver.WaitAndFindElements(By.XPath(buyNowForSpanXPath), 10);

        var tcgPlayerButton = driver.FindElement(By.XPath(tcgPlayerButtonXPath));
        tcgPlayerButton.Click();

        var buyNowForSpans = driver.FindElements(By.XPath(buyNowForSpanXPath));

        var dollarIndex = buyNowForSpans[0].Text.IndexOf('$');
        var priceSubstring = buyNowForSpans[0].Text.Substring(dollarIndex + 1).Trim();
        var price = Convert.ToDouble(priceSubstring, new CultureInfo("en-US"));
        return price;
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
            if (link == null) continue;

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
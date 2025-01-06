using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MtgDeckInfoScraper.Scraping.Abstractions;
using MtgDeckInfoScraper.Scraping.Models;

namespace MtgDeckInfoScraper.Scraping.Moxfield;

public class MoxfieldDeckInfoScraper : IDeckInfoScraper
{
    private const bool Headless = false;
    private readonly ILogger<MoxfieldDeckInfoScraper> _logger;

    public MoxfieldDeckInfoScraper(ILogger<MoxfieldDeckInfoScraper> logger)
    {
        _logger = logger;
    }

    public async Task<List<DeckInfo>> ScrapeDeckList(string deckListUrl, int count = 10)
    {
        _logger.LogInformation("Starting to scrape deck list from URL: {DeckListUrl} with count: {Count}", deckListUrl,
            count);

        var deckLinks = await ScrapeDeckLinks(deckListUrl);
        _logger.LogInformation("Retrieved {DeckLinkCount} deck links", deckLinks.Count);

        var toCheck = count > deckLinks.Count ? deckLinks : deckLinks.Take(count);
        var deckList = new List<DeckInfo>();

        int chunkCount = 0;
        int deckCount = 0;
        foreach (var deckLinkChunk in toCheck.Chunk(5))
        {
            chunkCount++;
            _logger.LogInformation("Processing chunk {ChunkNumber}", chunkCount);

            var scrapeTasks = new List<Task>();
            foreach (var deckLink in deckLinkChunk)
            {
                deckCount++;
                var currentDeckCount = deckCount;
                _logger.LogInformation("Starting scrape for deck {DeckNumber}: {DeckLink}", currentDeckCount, deckLink);

                scrapeTasks.Add(Task.Run(async () =>
                {
                    var info = await NavigateToAndScrapeDeckInfo(deckLink);
                    _logger.LogDebug("Scraped deck info for {DeckNumber}: {DeckInfo}", currentDeckCount, info);
                    lock (deckList)
                    {
                        deckList.Add(info);
                    }
                }));
            }

            await Task.WhenAll(scrapeTasks);
        }

        _logger.LogInformation("Completed scraping. Total decks scraped: {TotalDecks}", deckList.Count);
        return deckList;
    }

    private async Task SignIn(IPage page)
    {
        await page.Locator("a").Filter(new() { HasText = "Sign In" }).ClickAsync();
        await page.WaitForSelectorAsync("input[id='username']");
        // Wait for use input...
        await Task.Delay(30000);
        await page.Locator("span").Filter(new() { HasText = "Sign In" }).ClickAsync();
    }

    private async Task<DeckInfo> NavigateToAndScrapeDeckInfo(string deckUrl)
    {
        _logger.LogInformation("Navigating to deck URL: {DeckUrl}", deckUrl);

        var userDataDir = "C:\\Users\\FK6216\\AppData\\Local\\Google\\Chrome\\User Data\\Default";

        using var playwright = await Playwright.CreateAsync();
        _logger.LogDebug("Playwright instance created.");

        await using var browser = await playwright.Chromium.LaunchPersistentContextAsync(userDataDir, new()
        {
            Headless = Headless,
            SlowMo = 1000,
        });
        _logger.LogDebug("Chromium browser launched with headless mode: {HeadlessMode}", Headless);

        var page = await browser.NewPageAsync();
        _logger.LogDebug("New browser page created.");
        _logger.LogInformation("Navigating to the URL: {DeckUrl}", deckUrl);
        await page.GotoAsync(deckUrl);

        await SignIn(page);

        _logger.LogDebug("Waiting for selector 'article[class=\"deckview\"]' to be available.");
        await page.WaitForSelectorAsync("section[class='deckview']");
        _logger.LogDebug("Selector 'article[class=\"deckview\"]' loaded successfully.");

        _logger.LogDebug("Starting to scrape deck title.");
        var title = await ScrapeDeckTitle(page);
        _logger.LogDebug("Deck title scraped: {Title}", title);

        _logger.LogDebug("Starting to scrape deck price.");
        var price = await ScrapeDeckPrice(page);
        _logger.LogDebug("Deck price scraped: {Price}", price);

        _logger.LogDebug("Starting to scrape last updated timestamp.");
        var lastUpdated = await ScrapeLastUpdated(page);
        _logger.LogDebug("Last updated timestamp scraped: {LastUpdated}", lastUpdated);

        _logger.LogDebug("Starting to scrape deck likes.");
        var likes = await ScrapeLikes(page);
        _logger.LogDebug("Deck likes scraped: {Likes}", likes);

        _logger.LogDebug("Starting to scrape deck views.");
        var views = await ScrapeViews(page);
        _logger.LogDebug("Deck views scraped: {Views}", views);

        var deckInfo = new DeckInfo(title, deckUrl, price, lastUpdated, likes, views);
        _logger.LogDebug("Constructed DeckInfo object: {DeckInfo}", deckInfo);

        return deckInfo;
    }

    private async Task<List<string>> ScrapeDeckLinks(string deckListUrl)
    {
        _logger.LogInformation("Scraping deck links from URL: {DeckListUrl}", deckListUrl);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = Headless,
        });

        var page = await browser.NewPageAsync();
        await page.GotoAsync(deckListUrl);

        await page.WaitForSelectorAsync("div[class='browse-ad-layout']");
        _logger.LogInformation("Waiting for deck list to load");

        var links = await page.QuerySelectorAllAsync("a[href*='/decks/']");
        var hrefs = await Task.WhenAll(links.Select(async link => await link.GetAttributeAsync("href")));

        var validLinks = hrefs
            .Where(link => link is not null && link.Contains("/decks/") &&
                           !link.Contains("/decks/public") &&
                           !link.Contains("/decks/liked") &&
                           !link.Contains("/decks/following"))
            .Select(link => "https://www.moxfield.com" + link)
            .ToList();

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

    private async Task<string> SafeScrape(IPage page, string selector, string fieldName, string defaultValue)
    {
        try
        {
            var element = await page.QuerySelectorAsync(selector);
            return await element?.TextContentAsync() ?? defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scrape {FieldName}. Returning default value: {DefaultValue}", fieldName,
                defaultValue);
            return defaultValue;
        }
    }

    private async Task<double> ScrapeDeckPrice(IPage page)
    {
        _logger.LogInformation("Scraping deck price...");
        try
        {
            await page.Locator("a").Filter(new() { HasText = "BuyDeck" }).ClickAsync();

            var buyNowLocator = page.GetByRole(AriaRole.Button, new() { Name = "Buy Now for" });
            await buyNowLocator.WaitForAsync();
            await page.Locator("label").Filter(new() { HasText = "TCGplayer" }).ClickAsync();

            var buyNowForText = await buyNowLocator.TextContentAsync();
            if (buyNowForText is null) return 0;

            var dollarIndex = buyNowForText.IndexOf('$');
            var priceSubstring = buyNowForText.Substring(dollarIndex + 1).Trim();
            var price = Convert.ToDouble(priceSubstring, new CultureInfo("en-US"));
            _logger.LogDebug("Scraped price: ${Price}", price);

            return price;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scrape deck price. Returning default value of 0.");
            return 0;
        }
    }
}
using MtgDeckInfoScraper.Scraping.Models;

namespace MtgDeckInfoScraper.App.LocalStorage;

public class CurrentScrapeProvider(ILocalStorageAccessor localStorageAccessor)
{
    private const string CurrentScrape = "CurrentScrape";

    public async Task<List<DeckInfo>?> GetCurrentScrape()
    {
        return await localStorageAccessor.GetItemAsync<List<DeckInfo>>(CurrentScrape);
    }

    public async Task SetCurrentScrape(List<DeckInfo> deckInfos)
    {
        await localStorageAccessor.SetItemAsync(CurrentScrape, deckInfos);
    }
}
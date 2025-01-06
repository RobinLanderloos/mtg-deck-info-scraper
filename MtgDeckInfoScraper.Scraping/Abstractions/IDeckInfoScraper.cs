using MtgDeckInfoScraper.Scraping.Models;

namespace MtgDeckInfoScraper.Scraping.Abstractions;

public interface IDeckInfoScraper
{
   public Task<List<DeckInfo>> ScrapeDeckList(string deckListUrl,
		int count = 10);
}
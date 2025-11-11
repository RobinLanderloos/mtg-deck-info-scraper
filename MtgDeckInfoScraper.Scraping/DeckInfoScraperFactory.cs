using Microsoft.Extensions.DependencyInjection;
using MtgDeckInfoScraper.Scraping.Abstractions;
using MtgDeckInfoScraper.Scraping.Archidekt;
using MtgDeckInfoScraper.Scraping.Moxfield;

namespace MtgDeckInfoScraper.Scraping;

public class DeckInfoScraperFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DeckInfoScraperFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDeckInfoScraper Create(string deckUrl)
    {
        var host = new Uri(deckUrl).Host.ToLowerInvariant();

        return host switch
        {
            "www.moxfield.com" or "moxfield.com" => _serviceProvider.GetRequiredService<MoxfieldDeckInfoScraper>(),
            "www.archidekt.com" or "archidekt.com" => _serviceProvider.GetRequiredService<ArchidektDeckInfoScraper>(),
            _ => throw new NotImplementedException($"No scraper implemented for site: {host}")
        };
    }
}
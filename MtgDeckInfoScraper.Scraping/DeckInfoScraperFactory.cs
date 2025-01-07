using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MtgDeckInfoScraper.Scraping.Abstractions;
using MtgDeckInfoScraper.Scraping.Moxfield;

namespace MtgDeckInfoScraper.Scraping;

public class DeckInfoScraperFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DeckInfoScraperFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDeckInfoScraper Create(SupportedSites site)
    {
        return site switch
        {
            SupportedSites.Moxfield => _serviceProvider.GetRequiredService<MoxfieldDeckInfoScraper>(),
            _ => throw new NotImplementedException()
        };
    }
}

public enum SupportedSites
{
    Moxfield
}
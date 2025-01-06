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
            SupportedSites.Moxfield => new MoxfieldDeckInfoScraper(_serviceProvider
                .GetRequiredService<ILogger<MoxfieldDeckInfoScraper>>()),
            _ => throw new NotImplementedException()
        };
    }
    
    private MoxfieldDeckInfoScraper CreateMoxfieldDeckInfoScraper()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<MoxfieldDeckInfoScraper>>();
        return new MoxfieldDeckInfoScraper(logger);
    }
}

public enum SupportedSites
{
    Moxfield
}
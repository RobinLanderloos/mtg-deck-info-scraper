using Microsoft.Extensions.DependencyInjection;
using MtgDeckInfoScraper.Scraping.Archidekt;
using MtgDeckInfoScraper.Scraping.Moxfield;

namespace MtgDeckInfoScraper.Scraping;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterScrapers(this IServiceCollection services)
    {
        services.AddTransient<MoxfieldDeckInfoScraper>();
        services.AddScoped<DeckInfoScraperFactory>();
        services.AddTransient<ArchidektDeckInfoScraper>();
        return services;
    }
}
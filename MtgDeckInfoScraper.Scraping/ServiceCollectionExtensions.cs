using Microsoft.Extensions.DependencyInjection;
using MtgDeckInfoScraper.Scraping.Moxfield;

namespace MtgDeckInfoScraper.Scraping;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterScrapers(this IServiceCollection services)
    {
        services.AddScoped<MoxfieldDeckInfoScraper>();
        services.AddScoped<DeckInfoScraperFactory>();
        return services;
    }
}
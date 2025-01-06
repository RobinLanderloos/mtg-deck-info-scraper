using Microsoft.Extensions.DependencyInjection;

namespace MtgDeckInfoScraper.Scraping;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterScrapers(this IServiceCollection services)
    {
        services.AddScoped<DeckInfoScraperFactory>();
        return services;
    }
}
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using MtgDeckInfoScraper.App.LocalStorage;
using MtgDeckInfoScraper.App.Preferences;
using MtgDeckInfoScraper.Scraping;
using MtgDeckInfoScraper.Scraping.Moxfield;
using MtgDeckInfoScraper.Scraping.Options;

namespace MtgDeckInfoScraper.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddLogging(logging =>
        {
            logging.AddDebug();
        });

        builder.Services.AddSingleton<IMtgDeckInfoScraperOptions, MtgDeckInfoScraperOptions>();
        builder.Services.RegisterScrapers();
        builder.Services.RegisterLocalStorageAccess();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright exited with code {exitCode}");
        }

        return builder.Build();
    }
    
    private static IServiceCollection RegisterLocalStorageAccess(this IServiceCollection services)
    {
        services.AddBlazoredLocalStorage();
        services.AddScoped<ILocalStorageAccessor, BlazoredLocalStorageAccessor>();
        services.AddScoped<CurrentScrapeProvider>();
        return services;
    }
}
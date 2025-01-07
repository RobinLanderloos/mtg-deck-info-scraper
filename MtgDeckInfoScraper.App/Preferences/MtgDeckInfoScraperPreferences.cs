using MtgDeckInfoScraper.Scraping.Options;

namespace MtgDeckInfoScraper.App.Preferences;

public class MtgDeckInfoScraperOptions : IMtgDeckInfoScraperOptions
{
    /// <summary>
    /// The chrome remote debugging port to use
    /// </summary>
    public int ChromeRemoteDebuggingPort
    {
        get => Microsoft.Maui.Storage.Preferences.Default.Get(nameof(ChromeRemoteDebuggingPort), 9222);
        set => Microsoft.Maui.Storage.Preferences.Default.Set(nameof(ChromeRemoteDebuggingPort), value);
    }

    /// <summary>
    /// Path to Chrome executable, will be used to launch chrome with remote debugging port
    /// </summary>
    public string ChromeExecutablePath
    {
        get => Microsoft.Maui.Storage.Preferences.Default.Get(nameof(ChromeExecutablePath), @"C:\Program Files\Google\Chrome\Application\chrome.exe");
        set => Microsoft.Maui.Storage.Preferences.Default.Set(nameof(ChromeExecutablePath), value);
    }

    /// <summary>
    /// Chrome user data profile name
    /// Will be created if it doesn't exist
    ///
    /// `C:\Users\{username}\AppData\Local\Google\Chrome\User Data\{ChromeUserDataProfileName}`
    /// </summary>
    public string ChromeUserDataProfileName
    {
        get => Microsoft.Maui.Storage.Preferences.Default.Get(nameof(ChromeUserDataProfileName), "MtgDeckInfoScraper");
        set => Microsoft.Maui.Storage.Preferences.Default.Set(nameof(ChromeUserDataProfileName), value);
    }
}

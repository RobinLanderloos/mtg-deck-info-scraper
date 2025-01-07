using Microsoft.Maui.Storage;

namespace MtgDeckInfoScraper.Scraping.Moxfield;

public class MtgDeckInfoScraperPreferences
{
    /// <summary>
    /// The chrome remote debugging port to use
    /// </summary>
    public int ChromeRemoteDebuggingPort
    {
        get => Preferences.Default.Get(nameof(ChromeRemoteDebuggingPort), 9222);
        set => Preferences.Default.Set(nameof(ChromeRemoteDebuggingPort), value);
    }

    /// <summary>
    /// Path to Chrome executable, will be used to launch chrome with remote debugging port
    /// </summary>
    public string ChromeExecutablePath
    {
        get => Preferences.Default.Get(nameof(ChromeExecutablePath), @"C:\Program Files\Google\Chrome\Application\chrome.exe");
        set => Preferences.Default.Set(nameof(ChromeExecutablePath), value);
    }

    /// <summary>
    /// Chrome user data profile name
    /// Will be created if it doesn't exist
    ///
    /// `C:\Users\{username}\AppData\Local\Google\Chrome\User Data\{ChromeUserDataProfileName}`
    /// </summary>
    public string ChromeUserDataProfileName
    {
        get => Preferences.Default.Get(nameof(ChromeUserDataProfileName), "MtgDeckInfoScraper");
        set => Preferences.Default.Set(nameof(ChromeUserDataProfileName), value);
    }
}
using MtgDeckInfoScraper.Scraping.Options;

namespace MtgDeckInfoScraper.App.Preferences;

public class MtgDeckInfoScraperOptions : IMtgDeckInfoScraperOptions
{
	/// <inheritdoc />
	public int ChromeRemoteDebuggingPort
	{
		get => Microsoft.Maui.Storage.Preferences.Default.Get(nameof(ChromeRemoteDebuggingPort), 9222);
		set => Microsoft.Maui.Storage.Preferences.Default.Set(nameof(ChromeRemoteDebuggingPort), value);
	}

	/// <inheritdoc />
	public string ChromeExecutablePath
	{
		get => Microsoft.Maui.Storage.Preferences.Default.Get(nameof(ChromeExecutablePath), @"C:\Program Files\Google\Chrome\Application\chrome.exe");
		set => Microsoft.Maui.Storage.Preferences.Default.Set(nameof(ChromeExecutablePath), value);
	}

	/// <inheritdoc />
	public string ChromeUserDataProfileName
	{
		get => Microsoft.Maui.Storage.Preferences.Default.Get(nameof(ChromeUserDataProfileName), "MtgDeckInfoScraper");
		set => Microsoft.Maui.Storage.Preferences.Default.Set(nameof(ChromeUserDataProfileName), value);
	}

	/// <inheritdoc />
	public int MoxfieldMaxConcurrentScrapes
	{
		get => Microsoft.Maui.Storage.Preferences.Default.Get(nameof(MoxfieldMaxConcurrentScrapes), 5);
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(MoxfieldMaxConcurrentScrapes), "Value must be at least 1.");
			Microsoft.Maui.Storage.Preferences.Default.Set(nameof(MoxfieldMaxConcurrentScrapes), value);
		}
	}

	public int ArchidektMaxConcurrentScrapes
	{
		get => Microsoft.Maui.Storage.Preferences.Default.Get(nameof(ArchidektMaxConcurrentScrapes), 3);
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(ArchidektMaxConcurrentScrapes), "Value must be at least 1.");
			Microsoft.Maui.Storage.Preferences.Default.Set(nameof(ArchidektMaxConcurrentScrapes), value);
		}
	}
}
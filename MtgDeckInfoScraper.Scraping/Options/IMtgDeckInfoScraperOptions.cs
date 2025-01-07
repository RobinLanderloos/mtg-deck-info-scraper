namespace MtgDeckInfoScraper.Scraping.Options;

public interface IMtgDeckInfoScraperOptions
{
	/// <summary>
	/// The chrome remote debugging port to use
	/// </summary>
	int ChromeRemoteDebuggingPort { get; set; }

	/// <summary>
	/// Path to Chrome executable, will be used to launch chrome with remote debugging port
	/// </summary>
	string ChromeExecutablePath { get; set; }

	/// <summary>
	/// Chrome user data profile name
	/// Will be created if it doesn't exist
	///
	/// `C:\Users\{username}\AppData\Local\Google\Chrome\User Data\{ChromeUserDataProfileName}`
	/// </summary>
	string ChromeUserDataProfileName { get; set; }
}
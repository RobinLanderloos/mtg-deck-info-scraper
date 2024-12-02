﻿using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MtgDeckInfoScraper.Server.scraping.Extensions;

public static class WebDriverExtensions
{
	public static IWebElement WaitAndFindElement(this IWebDriver driver,
		By by,
		int timeoutInSeconds)
	{
		if (timeoutInSeconds <= 0) return driver.FindElement(by);
		var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
		return wait.Until(drv => drv.FindElement(by));
	}

	public static IReadOnlyCollection<IWebElement> WaitAndFindElements(this IWebDriver driver,
		By by,
		int timeoutInSeconds)
	{
		if (timeoutInSeconds <= 0) return driver.FindElements(by);
		var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
		wait.Until(drv => drv.FindElements(by).Count > 0);

		return driver.FindElements(by);
	}
}
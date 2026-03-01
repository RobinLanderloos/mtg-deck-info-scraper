# Plan 4 — Add unit/integration tests and build validation

## Problem
The `maxCost` feature introduced branching logic in both scrapers and a shared base class, but
there is no automated test coverage and no CI build-validation step. Regressions could go
undetected when the scraping or filtering logic changes.

## Approach
- Add a new `MtgDeckInfoScraper.Tests` xUnit project targeting `net10.0`.
- Write **unit tests** for `BaseDeckInfoScraper` helpers (semaphore guard, max-cost sequential
  filter loop) using fakes/stubs — no Playwright or live network required.
- Write **integration smoke tests** for `MoxfieldDeckInfoScraper` and `ArchidektDeckInfoScraper`
  that verify the `maxCost == null` (concurrent) path and the `maxCost != null` (sequential)
  path both return the correct number and shape of results, using a mock `IPage` / stub HTTP
  fixture rather than a live browser.

## Files changed (5)
1. `MtgDeckInfoScraper.Tests/MtgDeckInfoScraper.Tests.csproj` *(new)*
2. `MtgDeckInfoScraper.Tests/Unit/BaseDeckInfoScraperTests.cs` *(new)*
3. `MtgDeckInfoScraper.Tests/Integration/MoxfieldDeckInfoScraperTests.cs` *(new)*
4. `MtgDeckInfoScraper.Tests/Integration/ArchidektDeckInfoScraperTests.cs` *(new)*

## Steps
1. Scaffold `MtgDeckInfoScraper.Tests.csproj` — reference `MtgDeckInfoScraper.Scraping`,
   add `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, and `Moq` packages,
   set `<IsTestProject>true</IsTestProject>`.
2. Write unit tests in `BaseDeckInfoScraperTests.cs`:
   - `ScrapeDeckListWithMaxCostAsync_FiltersDecksOverMaxCost` — stub a list of fake decks where
     some exceed the cost limit; assert only qualifying decks are returned.
   - `ScrapeDeckListWithMaxCostAsync_StopsEarlyWhenCountReached` — assert iteration halts once
     the requested count of qualifying decks is collected.
   - `ScrapeWithSemaphoreAsync_ReleasesOnException` — assert the semaphore is always released
     even when the inner delegate throws.
3. Write `MoxfieldDeckInfoScraperTests.cs` — mock `IPage` to return pre-baked CSS-selector
   results; test both paths (null and non-null `maxCost`); assert result count and that no
   deck in the max-cost path exceeds the price threshold.
4. Write `ArchidektDeckInfoScraperTests.cs` — same pattern as step 3 for Archidekt selectors.
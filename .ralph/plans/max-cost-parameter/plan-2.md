# Plan 2 — Refactor: Extract shared base class from both scrapers

## Problem
`MoxfieldDeckInfoScraper` and `ArchidektDeckInfoScraper` were developed independently and share
a significant amount of duplicated logic:
- Playwright instance creation + CDP browser connection (identical boilerplate)
- `SemaphoreSlim` construction and acquire/release pattern
- The upcoming sequential max-cost loop (would otherwise be duplicated in plan-3)

## Approach
Introduce an abstract `BaseDeckInfoScraper` in a new `Base/` folder under `MtgDeckInfoScraper.Scraping`.
The base class encapsulates:
- `ConnectToBrowserAsync()` — creates Playwright + connects over CDP
- `ScrapeWithSemaphoreAsync<T>(Func<Task<T>>)` — semaphore-guarded execution helper
- `ScrapeDeckListWithMaxCostAsync(...)` — the sequential filtered-scraping loop (to be used by both
  implementations after plan-1 lands on Moxfield and plan-3 lands on Archidekt)

Both concrete scrapers are then updated to extend `BaseDeckInfoScraper` and delegate the shared
logic, keeping only their site-specific CSS selector / parsing code.

## Files changed (3)
1. `MtgDeckInfoScraper.Scraping/Base/BaseDeckInfoScraper.cs` *(new)*
2. `MtgDeckInfoScraper.Scraping/Moxfield/MoxfieldDeckInfoScraper.cs`
3. `MtgDeckInfoScraper.Scraping/Archidekt/ArchidektDeckInfoScraper.cs`

## Steps
1. Create `BaseDeckInfoScraper` with the shared helpers listed above.
2. Migrate `MoxfieldDeckInfoScraper` to extend the base class; remove duplicated code.
3. Migrate `ArchidektDeckInfoScraper` to extend the base class; remove duplicated code.
4. Verify both scrapers compile and all existing behaviour is preserved.

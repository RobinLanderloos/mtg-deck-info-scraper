# Plan 3 — Add `maxCost` to `ArchidektDeckInfoScraper`

## Problem
After plan-2's refactoring, `ArchidektDeckInfoScraper` still needs to honour the `maxCost`
parameter introduced to the interface in plan-1.

## Approach
Leverage the `ScrapeDeckListWithMaxCostAsync` helper from `BaseDeckInfoScraper` (introduced in
plan-2) to add the sequential filtered-scraping behaviour to the Archidekt scraper, exactly
mirroring the Moxfield implementation from plan-1 — but now without code duplication.

## Files changed (1)
1. `MtgDeckInfoScraper.Scraping/Archidekt/ArchidektDeckInfoScraper.cs`

## Steps
1. Update `ScrapeDeckList` signature to include `double? maxCost = null`.
2. Call `ScrapeDeckListWithMaxCostAsync` from the base class when `maxCost != null`.
3. Retain existing concurrent batch path when `maxCost == null`.
4. Ensure null-safety (existing null-filter on results) is preserved in both paths.

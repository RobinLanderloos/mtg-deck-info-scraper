# Plan 1 — Add `maxCost` to `IDeckInfoScraper` + `MoxfieldDeckInfoScraper`

## Problem
`IDeckInfoScraper.ScrapeDeckList` only accepts a `count` parameter. We need to add an optional
`maxCost` parameter so the caller can ask for N decks **whose price does not exceed** the limit.

## Approach
- Add `double? maxCost = null` to `IDeckInfoScraper.ScrapeDeckList`.
- In `MoxfieldDeckInfoScraper.ScrapeDeckList`:
  - When `maxCost` is `null`, retain the current concurrent batch behaviour.
  - When `maxCost` is provided, iterate over the deck links **sequentially** (respecting the
    existing semaphore), scrape each deck, keep it only if `price ≤ maxCost`, and stop as soon
    as `count` qualifying decks have been collected (or all links are exhausted).

## Files changed (2)
1. `MtgDeckInfoScraper.Scraping/Abstractions/IDeckInfoScraper.cs`
2. `MtgDeckInfoScraper.Scraping/Moxfield/MoxfieldDeckInfoScraper.cs`

## Steps
1. Add `double? maxCost = null` parameter to `IDeckInfoScraper.ScrapeDeckList`.
2. In `MoxfieldDeckInfoScraper.ScrapeDeckList`, after obtaining `deckLinks`:
   - If `maxCost == null`: keep existing `Take(count)` + concurrent `Task.WhenAll` logic.
   - If `maxCost != null`: loop through all `deckLinks` sequentially, scrape each one,
     append to results list if `deckInfo.Price <= maxCost`, break when `results.Count == count`.
3. Return the collected list in both branches.

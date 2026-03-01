# Plan 4 — Add `maxCost` input to the Blazor UI

## Problem
The scrape form only exposes `URL` and `Scrape Count`. The new `maxCost` parameter must be
surfaced as an optional numeric input following local UI conventions (DaisyUI / Tailwind).

## Approach
- Add a nullable `double? _maxCost` field.
- Add a `form-control` block with a `Max Cost` label and a number input bound to `_maxCost`,
  positioned after the `Scrape Count` control.
- Pass `_maxCost` to `scraper.ScrapeDeckList`.

## Files changed (1)
1. `MtgDeckInfoScraper.App/Components/Pages/MoxfieldScraping.razor`

## Steps
1. Add `private double? _maxCost;` field in the `@code` block.
2. Insert a new `form-control` div (label "Max Cost", placeholder "Optional — leave blank for no limit")
   after the existing `scrapeCount` control.
3. Update the `ScrapeDeckList` call: `scraper.ScrapeDeckList(_url, _scrapeCount, _maxCost)`.

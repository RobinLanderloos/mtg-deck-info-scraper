# Plan Favorites 2 — `FavoriteButton` Component + Scrape Page Integration

## Problem
The Scrape page has no way for users to mark a deck as a favorite. We need a reusable
`FavoriteButton` component and a new column in the scrape results table.

## Approach
- Create `FavoriteButton.razor` under `Components/DataDisplay/`.
- Add the button as an extra column in `MoxfieldScraping.razor`.

## Files Changed (2)
1. `MtgDeckInfoScraper.App/Components/DataDisplay/FavoriteButton.razor` ← **new**
2. `MtgDeckInfoScraper.App/Components/Pages/MoxfieldScraping.razor` ← add column + inject provider

## Steps
1. Create `FavoriteButton.razor`:
   - `[Parameter, EditorRequired] public DeckInfo Deck { get; set; }`
   - `[Inject] public FavoritesProvider FavoritesProvider { get; set; }`
   - `OnInitializedAsync`: call `IsFavoriteAsync` and store result in `_isFavorite`.
   - Render a `<button>` with:
     - `fa-solid fa-heart` icon when `_isFavorite == true`
     - `fa-regular fa-heart` icon when `_isFavorite == false`
     - `@onclick` toggles the state: calls `AddFavoriteAsync` or `RemoveFavoriteAsync` then
       flips `_isFavorite` and calls `StateHasChanged()`.
2. In `MoxfieldScraping.razor`:
   - Inject `FavoritesProvider`.
   - Add an empty `<GridHeader>` (non-sortable) for the favorites column.
   - Add a `<td>` in `GridRowTemplate` containing `<FavoriteButton Deck="@context"/>`.

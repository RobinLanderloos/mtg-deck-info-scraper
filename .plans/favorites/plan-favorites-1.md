# Plan Favorites 1 — `FavoritesProvider` + DI Registration

## Problem
There is no persistence layer for favorited decks. We need a `FavoritesProvider` that mirrors the
pattern of `CurrentScrapeProvider`, stores a `List<DeckInfo>` under the key `"Favorites"` in
Blazored Local Storage, and is registered in the DI container.

## Approach
- Add `FavoritesProvider.cs` to `MtgDeckInfoScraper.App/LocalStorage/`.
- Register it as `Scoped` in `MauiProgram.RegisterLocalStorageAccess`.

## Files Changed (2)
1. `MtgDeckInfoScraper.App/LocalStorage/FavoritesProvider.cs` ← **new**
2. `MtgDeckInfoScraper.App/MauiProgram.cs` ← add registration

## Steps
1. Create `FavoritesProvider` with the following public API:
   - `GetFavoritesAsync() → Task<List<DeckInfo>>`
   - `AddFavoriteAsync(DeckInfo) → Task`  (no-op if already present by `Id`)
   - `RemoveFavoriteAsync(Guid deckInfoId) → Task`
   - `IsFavoriteAsync(Guid deckInfoId) → Task<bool>`
   All methods use `ILocalStorageAccessor` with key `"Favorites"`.
2. In `MauiProgram.RegisterLocalStorageAccess`, add:
   `services.AddScoped<FavoritesProvider>();`

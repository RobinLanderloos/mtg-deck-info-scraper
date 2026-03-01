# Plan Favorites 4 — Unit Tests + Build Verification

## Problem
No tests exist for `FavoritesProvider`. We need unit tests to validate its behaviour and must
confirm the whole solution still builds and the existing test suite still passes.

## Approach
- Add `FavoritesProviderTests.cs` to `MtgDeckInfoScraper.Tests/Unit/`, following the pattern
  used in `BaseDeckInfoScraperTests.cs`.
- Run `dotnet build` and `dotnet test` to confirm nothing is broken.

## Files Changed (1)
1. `MtgDeckInfoScraper.Tests/Unit/FavoritesProviderTests.cs` ← **new**

## Steps
1. Mock `ILocalStorageAccessor` (use whatever mock library is already in the test project).
2. Write tests for:
   - `GetFavoritesAsync` returns an empty list when storage is empty (`null`).
   - `AddFavoriteAsync` persists a new deck.
   - `AddFavoriteAsync` is idempotent — adding the same deck twice does not create duplicates.
   - `RemoveFavoriteAsync` removes only the targeted deck by `Id`.
   - `IsFavoriteAsync` returns `true` for a stored deck and `false` for an unknown `Id`.
3. Run `dotnet build` on the solution and confirm exit code 0.
4. Run `dotnet test` on the solution and confirm all tests pass.

# Plan Favorites 3 — Favorites Page + Nav Menu Entry

## Problem
There is no dedicated page to view and manage favorited decks, and no navigation link to reach it.

## Approach
- Create `Favorites.razor` at `@page "/Favorites"` that loads all favorites from `FavoritesProvider`
  and renders them in the same `Grid` component used on the Scrape page.
- Add a "Favorites" `NavMenuLink` to `NavMenu.razor`.

## Files Changed (2)
1. `MtgDeckInfoScraper.App/Components/Pages/Favorites.razor` ← **new**
2. `MtgDeckInfoScraper.App/Components/Layout/NavMenu.razor` ← add nav link

## Steps
1. Create `Favorites.razor`:
   - Inject `FavoritesProvider`.
   - `OnInitializedAsync`: load favorites into `_deckList`.
   - Display `<PageHeader Title="Favorites"/>`.
   - If `_deckList` is empty: show a friendly "No favorites yet" message.
   - Otherwise: render the same `Grid<DeckInfo>` / column layout as `MoxfieldScraping.razor`,
     with all the same columns (Title, Price, Likes, Views, LastUpdated), plus:
       - A `<FavoriteButton Deck="@context"/>` column for toggling (unfavoring removes the row
         immediately; wire an `OnUnfavorited` callback that calls `RemoveFavoriteAsync` and
         removes the entry from `_deckList`).
2. In `NavMenu.razor`, add between the existing "Settings" link and the "Start Chrome" button:
   ```razor
   <NavMenuLink Text="Favorites" Href="Favorites">
       <Icon><i class="fa-solid fa-heart"></i></Icon>
   </NavMenuLink>
   ```
   Use `NavLinkMatch.All` (the default).

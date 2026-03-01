using MtgDeckInfoScraper.Scraping.Models;

namespace MtgDeckInfoScraper.App.LocalStorage;

public class FavoritesProvider(ILocalStorageAccessor localStorageAccessor)
{
    private const string Favorites = "Favorites";

    public async Task<List<DeckInfo>> GetFavoritesAsync()
    {
        return await localStorageAccessor.GetItemAsync<List<DeckInfo>>(Favorites) ?? [];
    }

    public async Task AddFavoriteAsync(DeckInfo deckInfo)
    {
        var favorites = await GetFavoritesAsync();
        if (favorites.All(d => d.Id != deckInfo.Id))
        {
            favorites.Add(deckInfo);
            await localStorageAccessor.SetItemAsync(Favorites, favorites);
        }
    }

    public async Task RemoveFavoriteAsync(Guid deckInfoId)
    {
        var favorites = await GetFavoritesAsync();
        var removed = favorites.RemoveAll(d => d.Id == deckInfoId);
        if (removed > 0)
            await localStorageAccessor.SetItemAsync(Favorites, favorites);
    }

    public async Task<bool> IsFavoriteAsync(Guid deckInfoId)
    {
        var favorites = await GetFavoritesAsync();
        return favorites.Any(d => d.Id == deckInfoId);
    }
}

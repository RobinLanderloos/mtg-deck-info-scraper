using Moq;
using MtgDeckInfoScraper.Scraping.LocalStorage;
using MtgDeckInfoScraper.Scraping.Models;

namespace MtgDeckInfoScraper.Tests.Unit;

public class FavoritesProviderTests
{
    private static DeckInfo MakeDeck(string title = "Test Deck") =>
        new(title, "https://example.com", '$', 50.0, "today", 0, 0);

    [Fact]
    public async Task GetFavoritesAsync_ReturnsEmptyList_WhenStorageIsEmpty()
    {
        var accessor = new Mock<ILocalStorageAccessor>();
        accessor.Setup(a => a.GetItemAsync<List<DeckInfo>>("Favorites"))
                .ReturnsAsync((List<DeckInfo>?)null);

        var provider = new FavoritesProvider(accessor.Object);
        var result = await provider.GetFavoritesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddFavoriteAsync_PersistsNewDeck()
    {
        List<DeckInfo>? stored = null;
        var accessor = new Mock<ILocalStorageAccessor>();
        accessor.Setup(a => a.GetItemAsync<List<DeckInfo>>("Favorites"))
                .ReturnsAsync(() => stored is null ? null : new List<DeckInfo>(stored));
        accessor.Setup(a => a.SetItemAsync("Favorites", It.IsAny<List<DeckInfo>>()))
                .Callback<string, List<DeckInfo>>((_, v) => stored = new List<DeckInfo>(v))
                .Returns(Task.CompletedTask);

        var provider = new FavoritesProvider(accessor.Object);
        var deck = MakeDeck();
        await provider.AddFavoriteAsync(deck);

        accessor.Verify(a => a.SetItemAsync("Favorites", It.Is<List<DeckInfo>>(l => l.Count == 1 && l[0].Id == deck.Id)), Times.Once);
    }

    [Fact]
    public async Task AddFavoriteAsync_IsIdempotent_WhenSameDeckAddedTwice()
    {
        List<DeckInfo>? stored = null;
        var accessor = new Mock<ILocalStorageAccessor>();
        accessor.Setup(a => a.GetItemAsync<List<DeckInfo>>("Favorites"))
                .ReturnsAsync(() => stored is null ? null : new List<DeckInfo>(stored));
        accessor.Setup(a => a.SetItemAsync("Favorites", It.IsAny<List<DeckInfo>>()))
                .Callback<string, List<DeckInfo>>((_, v) => stored = new List<DeckInfo>(v))
                .Returns(Task.CompletedTask);

        var provider = new FavoritesProvider(accessor.Object);
        var deck = MakeDeck();
        await provider.AddFavoriteAsync(deck);
        await provider.AddFavoriteAsync(deck);

        Assert.Single(stored!);
        accessor.Verify(a => a.SetItemAsync("Favorites", It.IsAny<List<DeckInfo>>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_RemovesOnlyTargetedDeck()
    {
        var deck1 = MakeDeck("Deck 1");
        var deck2 = MakeDeck("Deck 2");
        List<DeckInfo> stored = [deck1, deck2];

        var accessor = new Mock<ILocalStorageAccessor>();
        accessor.Setup(a => a.GetItemAsync<List<DeckInfo>>("Favorites"))
                .ReturnsAsync(() => new List<DeckInfo>(stored));
        accessor.Setup(a => a.SetItemAsync("Favorites", It.IsAny<List<DeckInfo>>()))
                .Callback<string, List<DeckInfo>>((_, v) => stored = new List<DeckInfo>(v))
                .Returns(Task.CompletedTask);

        var provider = new FavoritesProvider(accessor.Object);
        await provider.RemoveFavoriteAsync(deck1.Id);

        Assert.Single(stored);
        Assert.Equal(deck2.Id, stored[0].Id);
    }

    [Fact]
    public async Task IsFavoriteAsync_ReturnsTrue_ForStoredDeck()
    {
        var deck = MakeDeck();
        var accessor = new Mock<ILocalStorageAccessor>();
        accessor.Setup(a => a.GetItemAsync<List<DeckInfo>>("Favorites"))
                .ReturnsAsync([deck]);

        var provider = new FavoritesProvider(accessor.Object);
        var result = await provider.IsFavoriteAsync(deck.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task IsFavoriteAsync_ReturnsFalse_ForUnknownId()
    {
        var deck = MakeDeck();
        var accessor = new Mock<ILocalStorageAccessor>();
        accessor.Setup(a => a.GetItemAsync<List<DeckInfo>>("Favorites"))
                .ReturnsAsync([deck]);

        var provider = new FavoritesProvider(accessor.Object);
        var result = await provider.IsFavoriteAsync(Guid.NewGuid());

        Assert.False(result);
    }
}

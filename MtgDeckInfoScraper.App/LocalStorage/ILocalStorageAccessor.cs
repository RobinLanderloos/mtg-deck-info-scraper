namespace MtgDeckInfoScraper.App.LocalStorage;

public interface ILocalStorageAccessor
{
    Task<T?> GetItemAsync<T>(string key);
    Task SetItemAsync<T>(string key, T value);
    Task RemoveItemAsync(string key);
}
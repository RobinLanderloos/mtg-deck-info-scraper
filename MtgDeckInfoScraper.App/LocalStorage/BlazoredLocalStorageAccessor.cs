using Blazored.LocalStorage;

namespace MtgDeckInfoScraper.App.LocalStorage;

public class BlazoredLocalStorageAccessor : ILocalStorageAccessor
{
    private readonly ILocalStorageService _localStorage;

    public BlazoredLocalStorageAccessor(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        return await _localStorage.GetItemAsync<T>(key);
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        await _localStorage.SetItemAsync(key, value);
    }

    public async Task RemoveItemAsync(string key)
    {
        await _localStorage.RemoveItemAsync(key);
    }
}
using System;

namespace Starward.Services.Cache;

internal class MemoryCache
{

    private static MemoryCache _instance;

    public static MemoryCache Instance => _instance ??= new MemoryCache();


    private InMemoryStorage<object> _storage = new() { MaxItemCount = 100 };





    public T? GetItem<T>(string key, TimeSpan duration)
    {
        var item = _storage.GetItem(key, duration);
        if (item?.Item is T t)
        {
            return t;
        }
        else
        {
            return default;
        }
    }



    public void SetItem<T>(string key, T item)
    {
        _storage.SetItem(new InMemoryStorageItem<object>(key, DateTime.Now, item!));
    }



}

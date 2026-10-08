using Dapper;
using Microsoft.Extensions.Caching.Distributed;

namespace Starward.GameManagement.Service;

public class SqliteCache : IDistributedCache
{

    private const string SqlGet = """
        SELECT Value FROM CacheEntry WHERE Key = @key AND ExpireTime > @Now;
        """;

    private const string SqlSet = """
        INSERT INTO CacheEntry (Key, Value, ExpireTime) VALUES (@key, @value, @expireTime)
        ON CONFLICT (Key) DO UPDATE SET Value = excluded.Value, ExpireTime = excluded.ExpireTime;
        """;

    private const string SqlRemove = """
        DELETE FROM CacheEntry WHERE Key = @key;
        """;



    private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();


    public byte[]? Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var con = GameInstallDatabase.CreateConnection();
        return con.QuerySingleOrDefault<byte[]?>(SqlGet, new { key, Now });
    }


    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var con = GameInstallDatabase.CreateConnection();
        var cmd = new CommandDefinition(SqlGet, new { key, Now }, cancellationToken: token);
        return await con.QuerySingleOrDefaultAsync<byte[]?>(cmd).ConfigureAwait(false);
    }


    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);
        using var con = GameInstallDatabase.CreateConnection();
        con.Execute(SqlSet, new { key, value, expireTime = GetExpireTime(options) });
    }


    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);
        using var con = GameInstallDatabase.CreateConnection();
        var cmd = new CommandDefinition(SqlSet, new { key, value, expireTime = GetExpireTime(options) }, cancellationToken: token);
        await con.ExecuteAsync(cmd).ConfigureAwait(false);
    }


    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var con = GameInstallDatabase.CreateConnection();
        con.Execute(SqlRemove, new { key });
    }


    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var con = GameInstallDatabase.CreateConnection();
        var cmd = new CommandDefinition(SqlRemove, new { key }, cancellationToken: token);
        await con.ExecuteAsync(cmd).ConfigureAwait(false);
    }


    public void Refresh(string key)
    {
    }


    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }


    private static long GetExpireTime(DistributedCacheEntryOptions options)
    {
        if (options.AbsoluteExpiration.HasValue)
        {
            return options.AbsoluteExpiration.Value.ToUnixTimeMilliseconds();
        }
        else
        {
            return long.MaxValue;
        }
    }

}

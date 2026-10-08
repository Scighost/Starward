using Polly;
using Starward.Core.HoYoPlay;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Starward.GameManagement;

internal static class Helper
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull([NotNull] this object? obj, GameId gameId, string typeName)
    {
        if (obj is null)
        {
            throw new InvalidDataException($"{typeName} of game {gameId.GameBiz} ({gameId.Id}) is null.");
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>([NotNull] this T? obj, GameId gameId) where T : class
    {
        if (obj is null)
        {
            throw new InvalidDataException($"{typeof(T).Name} of game {gameId.GameBiz} ({gameId.Id}) is null.");
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T EnsureNotNull<T>(this T? obj, GameId gameId) where T : class
    {
        if (obj is null)
        {
            throw new InvalidDataException($"{typeof(T).Name} of game {gameId.GameBiz} ({gameId.Id}) is null.");
        }
        return obj;
    }


    public static async Task ForEachAsync<T>(this ResiliencePipeline pipeline, IEnumerable<T> source, CancellationToken cancellation, Func<T, CancellationToken, ValueTask> body)
    {
        await Parallel.ForEachAsync(source, cancellation, async (item, token) =>
        {
            await pipeline.ExecuteAsync(async ct => await body(item, ct), token).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }


    public static async Task ForEachAsync<T>(this ResiliencePipeline pipeline, IEnumerable<T> source, ParallelOptions parallelOptions, Func<T, CancellationToken, ValueTask> body)
    {
        await Parallel.ForEachAsync(source, parallelOptions, async (item, token) =>
        {
            await pipeline.ExecuteAsync(async ct => await body(item, ct), token).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }


    public static string GetUrlFileName(string url)
    {
        string name = Path.GetFileName(url);
        int i = name.IndexOf('?');
        if (i > 0)
        {
            name = name[..i];
        }
        return name;
    }


}
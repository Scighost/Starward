using Dapper;
using Starward.Core.HoYoPlay;
using Starward.Features.Database;
using System.Collections.Generic;
using System.Linq;

namespace Starward.Features.GameSelector;

public class GameEntryService
{


    public static List<GameEntry> GetAllEntries()
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<GameEntry>("SELECT * FROM GameEntry;").ToList();
    }


    public static List<GameEntry> GetPinnedEntries()
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<GameEntry>("SELECT * FROM GameEntry WHERE IsPinned > 0 ORDER BY PinnedOrder;").ToList();
    }


    public static List<GameEntry> GetInstalledEntries()
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<GameEntry>("SELECT * FROM GameEntry WHERE InstallPath IS NOT NULL ORDER BY GameBiz, LauncherId;").ToList();
    }


    public static GameEntry? GetFromId(GameId gameId)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.QuerySingleOrDefault<GameEntry>("SELECT * FROM GameEntry WHERE GameId = @Id;", gameId);
    }


    public static void SaveEntries(IList<GameEntry> entries)
    {
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        dapper.Execute("""
            INSERT INTO GameEntry (GameId, GameBiz, LauncherId, InstallPath, Removable, IsPinned, PinnedOrder)
            VALUES (@GameId, @GameBiz, @LauncherId, @InstallPath, @Removable, @IsPinned, @PinnedOrder)
            ON CONFLICT DO UPDATE SET InstallPath = excluded.InstallPath,
                                      Removable   = excluded.Removable
                                      IsPinned    = excluded.IsPinned
                                      PinnedOrder = excluded.PinnedOrder;
            """, t);
        t.Commit();
    }



    public static void SetInstallPath(GameId gameId, string launcherId, string? installPath, bool removable)
    {
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT INTO GameEntry (GameId, GameBiz, LauncherId, InstallPath, Removable, IsPinned, PinnedOrder)
            VALUES (@GameId, @GameBiz, @LauncherId, @InstallPath, @Removable, @IsPinned, @PinnedOrder)
            ON CONFLICT DO UPDATE SET InstallPath = excluded.InstallPath, Removable = excluded.Removable;
            """, new GameEntry
        {
            GameId = gameId.Id,
            GameBiz = gameId.GameBiz,
            LauncherId = launcherId,
            InstallPath = installPath,
            Removable = removable,
        });
    }



    public static void SetInstallPathNull(GameId gameId)
    {
        try
        {
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("UPDATE GameEntry SET InstallPath = NULL, Removable = 0 WHERE GameId = @gameId;", new { gameId = gameId.Id });
        }
        catch { }
    }


    public static void SavePinnedIcons(IList<PinnedGameIcon> icons)
    {
        List<GameEntry> list = new(icons.Count);
        for (int i = 0; i < icons.Count; i++)
        {
            PinnedGameIcon item = icons[i];
            list.Add(new GameEntry
            {
                GameId = item.GameId.Id,
                GameBiz = item.GameId.GameBiz,
                LauncherId = item.LauncherId,
                IsPinned = true,
                PinnedOrder = i + 1,
            });
        }
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        dapper.Execute("UPDATE GameEntry SET IsPinned = 0, PinnedOrder = 0 WHERE TRUE;");
        dapper.Execute("""
                INSERT INTO GameEntry (GameId, GameBiz, LauncherId, InstallPath, Removable, IsPinned, PinnedOrder)
                VALUES (@GameId, @GameBiz, @LauncherId, @InstallPath, @Removable, @IsPinned, @PinnedOrder)
                ON CONFLICT DO UPDATE SET IsPinned = excluded.IsPinned, PinnedOrder = excluded.PinnedOrder;
                """, list, t);
        dapper.Execute("DELETE FROM GameEntry WHERE InstallPath IS NULL AND IsPinned = 0;");
        t.Commit();
    }







}
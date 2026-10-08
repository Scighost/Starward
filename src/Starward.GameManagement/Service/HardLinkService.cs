using Dapper;
using Starward.Core.HoYoPlay;
using Vanara.PInvoke;

namespace Starward.GameManagement.Service;

public static class HardLinkService
{


    public static async Task<bool> TryHardLinkAsync(GameId gameId, string path, long size, string md5, CancellationToken cancellation = default)
    {
        try
        {
            path = Path.GetFullPath(path);
            if (new DriveInfo(path).DriveFormat is not "NTFS" and not "ReFS")
            {
                return false;
            }
            using var con = GameInstallDatabase.CreateConnection();
            IEnumerable<GameFileItem> list = con.Query<GameFileItem>("SELECT * FROM GameFile WHERE Path != @path AND Size = @size AND MD5 = @md5 ORDER BY LastWriteTime;", new { path, size, md5 });
            foreach (GameFileItem item in list)
            {
                if (!File.Exists(item.Path))
                {
                    DeleteGameFileItem(item.Path);
                    continue;
                }
                if (Path.GetPathRoot(path) != Path.GetPathRoot(item.Path))
                {
                    continue;
                }
                await UpdateGameFileItemAsync(item, cancellation);
                if (item.Size == size && string.Equals(item.MD5, md5, StringComparison.OrdinalIgnoreCase))
                {
                    if (TryHardLink(path, item.Path))
                    {
                        InsertGameFileItem(gameId, path, size, md5);
                        return true;
                    }
                }
            }
        }
        catch { }
        return false;
    }



    private static bool TryHardLink(string path, string pathToTarget)
    {
        string? folder = Path.GetDirectoryName(path);
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }
        string link = path + ".link";
        if (File.Exists(link))
        {
            File.Delete(link);
        }
        if (Kernel32.CreateHardLink(link, pathToTarget))
        {
            File.Move(link, path, true);
            return true;
        }
        else
        {
            return false;
        }
    }



    private static async Task UpdateGameFileItemAsync(GameFileItem item, CancellationToken cancellation = default)
    {
        var info = new FileInfo(item.Path);
        item.Size = info.Length;
        item.LastWriteTime = info.LastWriteTime.ToFileTimeUtc();
        item.MD5 = await HashService.GetFileMD5Async(item.Path, cancellation);
        using var handle = File.OpenHandle(item.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var id = Kernel32.GetFileInformationByHandleEx<Kernel32.FILE_ID_INFO>(handle, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
        item.VolumeId = BitConverter.GetBytes(id.VolumeSerialNumber);
        item.FileId = id.FileId.Identifier;
        InsertGameFileItem(item);
    }



    public static void InsertGameFileItem(GameId gameId, string path, long size, string md5)
    {
        try
        {
            FileInfo info = new(path);
            if (size != info.Length)
            {
                return;
            }
            GameFileItem item = new GameFileItem()
            {
                Path = path,
                GameId = gameId.Id,
                Size = size,
                MD5 = md5,
                LastWriteTime = info.LastWriteTime.ToFileTimeUtc(),
            };
            using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var id = Kernel32.GetFileInformationByHandleEx<Kernel32.FILE_ID_INFO>(handle, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
            item.VolumeId = BitConverter.GetBytes(id.VolumeSerialNumber);
            item.FileId = id.FileId.Identifier;
            InsertGameFileItem(item);
        }
        catch { }
    }



    private static void InsertGameFileItem(GameFileItem item)
    {
        item.Path = Path.GetFullPath(item.Path);
        using var con = GameInstallDatabase.CreateConnection();
        con.Execute(""""
            INSERT INTO GameFile (Path, GameId, VolumeId, FileId, Size, MD5, LastWriteTime)
            VALUES (@Path, @GameId, @VolumeId, @FileId, @Size, @MD5, @LastWriteTime)
            ON CONFLICT (Path) DO UPDATE SET GameId        = excluded.GameId,
                                             VolumeId      = excluded.VolumeId,
                                             FileId        = excluded.FileId,
                                             Size          = excluded.Size,
                                             MD5           = excluded.MD5,
                                             LastWriteTime = excluded.LastWriteTime;
            """", item);
    }


    public static void DeleteGameFileItem(string path)
    {
        try
        {
            path = Path.GetFullPath(path);
            using var con = GameInstallDatabase.CreateConnection();
            con.Execute("DELETE FROM GameFile WHERE Path = @path;", new { path });
        }
        catch { }
    }




    public static void DeleteGameFolder(string folder)
    {
        try
        {
            string prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)) + Path.DirectorySeparatorChar;
            using var con = GameInstallDatabase.CreateConnection();
            var list = con.Query<string>("SELECT Path FROM GameFile WHERE Path LIKE @prefix || '%';", new { prefix });
            var deletes = list.Where(x => x.StartsWith(folder)).ToList();
            con.Execute("DELETE FROM GameFile WHERE Path IN @deletes;", new { deletes });
        }
        catch { }
    }



}

public class GameFileItem
{

    public string Path { get; set; }

    public string? GameId { get; set; }

    public byte[]? VolumeId { get; set; }

    public byte[]? FileId { get; set; }

    public long Size { get; set; }

    public string? MD5 { get; set; }

    /// <summary>
    /// Windows FileTime: FileInfo.LastWriteTime.ToFileTimeUtc()
    /// </summary>
    public long LastWriteTime { get; set; }

}
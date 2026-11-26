using Dapper;
using Microsoft.Data.Sqlite;

namespace Starward.GameInstall;

public class GameFileDatabase
{


    private static string? _connectionString;


    private static readonly Lock _lock = new();


    public static string? DatabasePath { get; private set; }



    public static SqliteConnection CreateConnection()
    {
        string? connectionString = _connectionString;
        if (connectionString is null)
        {
            throw new InvalidOperationException($"Database is not initialized. Call {nameof(SetDatabase)} first.");
        }
        var con = new SqliteConnection(connectionString);
        con.Open();
        con.Execute("PRAGMA busy_timeout = 5000;");
        return con;
    }



    public static void SetDatabase(string folder)
    {
        if (Directory.Exists(folder))
        {
            string path = Path.GetFullPath(Path.Combine(folder, "GameFile.db"));
            lock (_lock)
            {
                DatabasePath = path;
                _connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = path,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Pooling = true,
                    DefaultTimeout = 10,
                }.ToString();
                InitializeDatabase();
            }
        }
    }



    private static void InitializeDatabase()
    {
        using var con = CreateConnection();
        int version = con.QueryFirstOrDefault<int>("PRAGMA USER_VERSION;");
        if (version == 0)
        {
            con.Execute("PRAGMA JOURNAL_MODE = WAL;");
        }
        foreach (var sql in DatabaseSqls.Skip(version))
        {
            con.Execute(sql);
        }
    }



    private static readonly List<string> DatabaseSqls =
    [
        Sql_v1,
    ];



    private const string Sql_v1 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS GameFile
        (
            Path          TEXT    NOT NULL PRIMARY KEY,
            GameId        TEXT    NULL,
            VolumeId      INTEGER NOT NULL DEFAULT 0,
            FileIdLow     INTEGER NOT NULL DEFAULT 0,
            FileIdHigh    INTEGER NOT NULL DEFAULT 0,
            Size          INTEGER NOT NULL DEFAULT 0,
            MD5           TEXT    NULL,
            LastWriteTime INTEGER NOT NULL DEFAULT 0
        );
        CREATE INDEX IF NOT EXISTS IX_GameFile_GameId ON GameFile (GameId);
        CREATE INDEX IF NOT EXISTS IX_GameFile_Size ON GameFile (Size);
        CREATE INDEX IF NOT EXISTS IX_GameFile_MD5 ON GameFile (MD5);
        CREATE INDEX IF NOT EXISTS IX_GameFile_LastWriteTime ON GameFile (LastWriteTime);
        CREATE INDEX IF NOT EXISTS IX_GameFile_VolumeId_FileId ON GameFile (VolumeId, FileIdLow, FileIdHigh);

        PRAGMA USER_VERSION = 1;
        COMMIT TRANSACTION;
        """;

}

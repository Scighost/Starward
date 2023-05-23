using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Starward.Services;

internal class DatabaseService
{

    private readonly ILogger<DatabaseService> _logger;


    private readonly string _databasePath;


    private readonly string _connectionString;




    public DatabaseService(ILogger<DatabaseService> logger, string databaseFolder)
    {
        _logger = logger;
        _databasePath = Path.Combine(databaseFolder, "StarwardDatabase.db");
        _logger.LogInformation($"Database path is '{_databasePath}'");
        _connectionString = $"DataSource={_databasePath};";
        InitializeDatabase();
    }




    public SqliteConnection CreateConnection()
    {
        var con = new SqliteConnection(_connectionString);
        con.Open();
        return con;
    }





    private void InitializeDatabase()
    {
        using var con = CreateConnection();
        var version = con.QueryFirstOrDefault<int>("PRAGMA USER_VERSION;");
        _logger.LogInformation($"Database version is {version}, target version is {StructureSqls.Count}.");
        if (version == 0)
        {
            con.Execute("PRAGMA JOURNAL_MODE = WAL;");
        }
        foreach (var sql in StructureSqls.Skip(version))
        {
            con.Execute(sql);
        }
    }



    public void AutoBackupDatabase()
    {
        try
        {
            if (AppConfig.EnableAutoBackupDatabase)
            {
                var interval = Math.Clamp(AppConfig.BackupIntervalInDays, 1, int.MaxValue);
                GetValue<string>("AutoBackupDatabase", out var lastTime);
                if ((DateTime.Now - lastTime).TotalDays > interval)
                {
                    var dir = Path.Combine(AppConfig.ConfigDirectory, "Backup");
                    Directory.CreateDirectory(dir);
                    var file = Path.Combine(dir, $"Database_{DateTime.Now:yyyyMMdd}.db");
                    using var backupCon = new SqliteConnection($"DataSource={file};");
                    backupCon.Open();
                    using var con = CreateConnection();
                    con.Execute("VACUUM;");
                    con.BackupDatabase(backupCon);
                    SetValue("AutoBackupDatabase", file);
                }
            }
        }
        catch { }
    }




    private class KVT
    {

        public KVT() { }


        public KVT(string key, string value, DateTime time)
        {
            Key = key;
            Value = value;
            Time = time;
        }

        public string Key { get; set; }

        public string Value { get; set; }

        public DateTime Time { get; set; }
    }





    public T? GetValue<T>(string key, out DateTime time, T? defaultValue = default)
    {
        time = DateTime.MinValue;
        try
        {
            using var con = CreateConnection();
            var kvt = con.QueryFirstOrDefault<KVT>("SELECT * FROM KVT WHERE Key = @key LIMIT 1;", new { key });
            if (kvt != null)
            {
                time = kvt.Time;
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null)
                {
                    return defaultValue;
                }
                return (T?)converter.ConvertFromString(kvt.Value);
            }
            else
            {
                return defaultValue;
            }
        }
        catch
        {
            return defaultValue;
        }
    }



    public bool TryGetValue<T>(string key, out T? result, out DateTime time, T? defaultValue = default)
    {
        result = defaultValue;
        time = DateTime.MinValue;
        try
        {
            using var con = CreateConnection();
            var kvt = con.QueryFirstOrDefault<KVT>("SELECT * FROM KVT WHERE Key = @key LIMIT 1;", new { key });
            if (kvt != null)
            {
                time = kvt.Time;
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null)
                {
                    return false;
                }
                result = (T?)converter.ConvertFromString(kvt.Value);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }



    public void SetValue<T>(string key, T value, DateTime? time = null)
    {
        try
        {
            using var con = CreateConnection();
            con.Execute("INSERT OR REPLACE INTO KVT (Key, Value, Time) VALUES (@Key, @Value, @Time);", new KVT(key, value?.ToString() ?? "", time ?? DateTime.Now));

        }
        catch { }
    }





    private static List<string> StructureSqls = new() { Structure_v1 };


    private const string Structure_v1 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS KVT
        (
            Key   TEXT NOT NULL PRIMARY KEY,
            Value TEXT NOT NULL,
            Time  TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS GameAccount
        (
            SHA256  TEXT    NOT NULL PRIMARY KEY,
            GameBiz INTEGER NOT NULL,
            Uid     INTEGER NOT NULL,
            Name    TEXT    NOT NULL,
            Value   BLOB    NOT NULL,
            Time    TEXT    NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_GameAccount_GameBiz ON GameAccount (GameBiz);

        CREATE TABLE IF NOT EXISTS GachaLogUrl
        (
            GameBiz INTEGER NOT NULL,
            Uid     INTEGER NOT NULL,
            Url     TEXT    NOT NULL,
            Time    TEXT    NOT NULL,
            PRIMARY KEY (GameBiz, Uid)
        );

        CREATE TABLE IF NOT EXISTS GenshinGachaItem
        (
            Uid       INTEGER NOT NULL,
            Id        INTEGER NOT NULL,
            Name      TEXT    NOT NULL,
            Time      TEXT    NOT NULL,
            ItemId    INTEGER NOT NULL,
            ItemType  TEXT    NOT NULL,
            RankType  INTEGER NOT NULL,
            GachaType INTEGER NOT NULL,
            Count     INTEGER NOT NULL,
            Lang      TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_Id ON GenshinGachaItem (Id);
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_RankType ON GenshinGachaItem (RankType);
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_GachaType ON GenshinGachaItem (GachaType);

        CREATE TABLE IF NOT EXISTS StarRailGachaItem
        (
            Uid       INTEGER NOT NULL,
            Id        INTEGER NOT NULL,
            Name      TEXT    NOT NULL,
            Time      TEXT    NOT NULL,
            ItemId    INTEGER NOT NULL,
            ItemType  TEXT    NOT NULL,
            RankType  INTEGER NOT NULL,
            GachaType INTEGER NOT NULL,
            GachaId   INTEGER NOT NULL,
            Count     INTEGER NOT NULL,
            Lang      TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_Id ON StarRailGachaItem (Id);
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_RankType ON StarRailGachaItem (RankType);
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_GachaType ON StarRailGachaItem (GachaType);

        PRAGMA USER_VERSION = 1;
        COMMIT TRANSACTION;
        """;




}
